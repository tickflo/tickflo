import { and, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  permissions,
  rolePermissions,
  roles,
  userWorkspaceRoles,
} from '~/.server/db/schema';
import { loginRedirect } from '~/.server/helpers';
import {
  type Permissions,
  defaultUserPermissions,
} from '~/.server/permissions';

export async function getPermissions(context: Context): Promise<Permissions> {
  const { user, session, tx } = context;

  if (user.isNone()) {
    throw loginRedirect(session);
  }

  const results = await (tx || db)
    .selectDistinct({
      resource: permissions.resource,
      action: permissions.action,
    })
    .from(permissions)
    .innerJoin(
      rolePermissions,
      eq(rolePermissions.permissionId, permissions.id),
    )
    .innerJoin(roles, eq(roles.id, rolePermissions.roleId))
    .innerJoin(
      userWorkspaceRoles,
      and(
        eq(userWorkspaceRoles.userId, user.value.id),
        eq(userWorkspaceRoles.roleId, roles.id),
      ),
    );

  const perms = defaultUserPermissions;

  for (const permission of results) {
    // @ts-ignore
    perms[permission.resource][permission.action] = true;
  }

  return perms;
}
