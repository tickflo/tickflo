import { and, eq, inArray } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { rolePermissions } from '~/.server/db/schema';
import { ApiError } from '~/.server/errors';
import type { Permissions } from '~/.server/permissions';

export async function updatePermissionsForRole(
  { roleId, permissions }: { roleId: number; permissions: Permissions },
  context: Context,
) {
  const { tx } = context;
  const user = context.user.unwrap();

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
      } else if (allowed) {
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
}
