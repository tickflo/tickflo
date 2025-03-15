import { eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { getPermissions, getRoleById } from '../security';
import { getUsersCountByRole } from '../user';
import { getWorkspaceBySlug } from '../workspace';

type Request = {
  roleId: number;
  slug: string;
};

export async function removeRole(
  { roleId, slug }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  if (Number.isNaN(roleId)) {
    return Err(new InputError('Invalid role id'));
  }

  const { tx } = context;

  const permissions = await getPermissions({ slug }, context);
  if (!permissions.roles.delete) {
    return Err(
      new PermissionsError('You do not have permission to remove roles'),
    );
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);

  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  const role = await getRoleById({ id: roleId, slug }, context);
  if (role.isErr()) {
    return Err(role.error);
  }

  const usersByRoles = await getUsersCountByRole({ slug }, context);
  const roleGroup = usersByRoles.find((g) => g.name === role.value.name);
  if (roleGroup && roleGroup.count > 0) {
    return Err(
      new InputError(
        'You must remove this role from all users before you can remove it',
      ),
    );
  }

  await (tx || db).delete(roles).where(eq(roles.id, roleId));

  return Ok.EMPTY;
}
