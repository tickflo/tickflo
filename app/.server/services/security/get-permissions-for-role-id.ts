import { and, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  permissions,
  rolePermissions,
  roles,
  workspaces,
} from '~/.server/db/schema';
import {
  type Permissions,
  defaultUserPermissions,
} from '~/.server/permissions';

export async function getPermissionsForRoleId(
  { id, slug }: { id: number; slug: string },
  context: Context,
): Promise<Permissions> {
  const { tx } = context;

  const results = await (tx || db)
    .selectDistinct({
      resource: permissions.resource,
      action: permissions.action,
    })
    .from(permissions)
    .innerJoin(
      rolePermissions,
      and(
        eq(rolePermissions.permissionId, permissions.id),
        eq(rolePermissions.roleId, id),
      ),
    )
    .innerJoin(roles, eq(roles.id, rolePermissions.roleId))
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, roles.workspaceId), eq(workspaces.slug, slug)),
    );

  const perms = defaultUserPermissions();

  for (const permission of results) {
    // @ts-ignore
    perms[permission.resource][permission.action] = true;
  }

  return perms;
}
