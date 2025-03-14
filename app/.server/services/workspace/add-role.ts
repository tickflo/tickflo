import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles } from '~/.server/db/schema';
import { ApiError, InputError, PermissionsError } from '~/.server/errors';
import type { Permissions } from '~/.server/permissions';
import { getPermissions, updatePermissionsForRole } from '../security';
import { getRoleByName } from './get-role-by-name';
import { getWorkspaceBySlug } from './get-workspace-by-slug';

export async function addRole(
  {
    slug,
    name,
    permissions: rolePermissions,
  }: {
    slug: string;
    name: string | undefined;
    permissions: Permissions;
  },
  context: Context,
): Promise<Result<void, ApiError>> {
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.roles.create) {
    return Err(new PermissionsError('You do not have permission to add roles'));
  }

  const { config } = context;

  if (
    !name ||
    name.length < config.ROLE.MIN_NAME_LENGTH ||
    name.length > config.ROLE.MAX_NAME_LENGTH
  ) {
    return Err(
      new InputError(
        `Name must be between ${config.ROLE.MIN_NAME_LENGTH} and ${config.ROLE.MAX_NAME_LENGTH} characters in length`,
      ),
    );
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);
  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  const existingRole = await getRoleByName({ slug, name }, context);
  if (existingRole.isSome()) {
    return Err(new InputError('A role with that name already exists'));
  }

  const user = context.user.unwrap();
  await db.transaction(async (tx) => {
    const result = await tx
      .insert(roles)
      .values({
        role: name,
        workspaceId: workspace.value.id,
        createdBy: user.id,
      })
      .returning({
        id: roles.id,
      });

    if (!result.length) {
      return Err(new ApiError('Failed to insert role record'));
    }

    const { id: roleId } = result[0];

    await updatePermissionsForRole(
      { roleId, permissions: rolePermissions },
      { ...context, tx },
    );
  });

  return Ok.EMPTY;
}
