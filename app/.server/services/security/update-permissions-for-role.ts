import { and, eq, inArray } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { rolePermissions } from '~/.server/db/schema';
import { ApiError, PermissionsError } from '~/.server/errors';
import type { Permissions } from '~/.server/permissions';
import { getRoleById } from '../security';
import { getPermissions } from './get-permissions';

export async function updatePermissionsForRole(
  {
    roleId,
    permissions,
    slug,
  }: { roleId: number; permissions: Permissions; slug: string },
  context: Context,
): Promise<Result<void, ApiError>> {
  const userPermissions = await getPermissions({ slug }, context);
  if (!(userPermissions.roles.update || userPermissions.roles.create)) {
    return Err(
      new PermissionsError('You do not have permission to update permissions'),
    );
  }

  const { tx } = context;
  const user = context.user.unwrap();

  const role = await getRoleById({ id: roleId, slug }, context);
  if (role.isErr()) {
    return Err(role.error);
  }

  // We don't need to save admin permissions
  if (role.value.admin) {
    return Ok.EMPTY;
  }

  const permissionIds = await (tx || db).query.permissions.findMany();

  const existingPermissions = await (tx || db).query.rolePermissions.findMany({
    where: eq(rolePermissions.roleId, roleId),
  });

  const deleteIds = [];
  const createIds = [];

  for (const resource in permissions) {
    // @ts-ignore
    const actions = permissions[resource];
    for (const action in actions) {
      const allowed: boolean = actions[action];
      const permissionIdRow = permissionIds.find(
        (r) => r.resource === resource && r.action === action,
      );
      if (!permissionIdRow) {
        throw new ApiError(
          `Could not find permission id for ${resource}.${action}`,
        );
      }

      const existing = existingPermissions.find(
        (r) => r.permissionId === permissionIdRow.id,
      );
      if (existing && !allowed) {
        deleteIds.push(existing.permissionId);
      } else if (!existing && allowed) {
        createIds.push(permissionIdRow.id);
      }
    }
  }

  if (deleteIds.length) {
    await (tx || db)
      .delete(rolePermissions)
      .where(
        and(
          eq(rolePermissions.roleId, roleId),
          inArray(rolePermissions.permissionId, deleteIds),
        ),
      );
  }

  if (createIds.length) {
    await (tx || db).insert(rolePermissions).values(
      createIds.map((id) => ({
        roleId,
        permissionId: id,
        createdBy: user.id,
      })),
    );
  }

  return Ok.EMPTY;
}
