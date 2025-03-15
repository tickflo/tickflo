import { eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles } from '~/.server/db/schema';
import { ApiError, InputError, PermissionsError } from '~/.server/errors';
import type { Permissions } from '~/.server/permissions';
import {
  getPermissions,
  getRoleById,
  updatePermissionsForRole,
} from '../security';
import { getWorkspaceBySlug } from '../workspace';

export async function updateRole(
  {
    id,
    slug,
    name,
    admin,
    permissions: rolePermissions,
  }: {
    id: number;
    slug: string;
    name: string | undefined;
    admin: boolean;
    permissions: Permissions;
  },
  context: Context,
): Promise<Result<void, ApiError>> {
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.roles.update) {
    return Err(
      new PermissionsError('You do not have permission to edit roles'),
    );
  }

  if (Number.isNaN(id)) {
    return Err(new InputError('Invalid role id'));
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

  if ((await getRoleById({ slug, id }, context)).isErr()) {
    return Err(new InputError('Invalid role id'));
  }

  const user = context.user.unwrap();
  await db.transaction(async (tx) => {
    const result = await tx
      .update(roles)
      .set({
        name,
        admin,
        updatedBy: user.id,
      })
      .where(eq(roles.id, id));

    if (!result.rowCount) {
      return Err(new ApiError('Failed to update role record'));
    }

    await updatePermissionsForRole(
      { roleId: id, permissions: rolePermissions, slug },
      { ...context, tx },
    );
  });

  return Ok.EMPTY;
}
