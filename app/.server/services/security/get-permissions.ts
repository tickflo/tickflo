import { and, eq, sql } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  permissions,
  rolePermissions,
  roles,
  userWorkspaceRoles,
  workspaces,
} from '~/.server/db/schema';
import {
  type Permissions,
  defaultAdminPermissions,
  defaultUserPermissions,
} from '~/.server/permissions';

export async function getPermissions(
  { slug }: { slug: string },
  context: Context,
): Promise<Permissions> {
  const { tx } = context;
  const user = context.user.unwrap();

  const userIsAdmin = await isAdmin({ slug }, context);
  if (userIsAdmin) {
    return defaultAdminPermissions();
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
        eq(userWorkspaceRoles.userId, user.id),
        eq(userWorkspaceRoles.roleId, roles.id),
      ),
    )
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaceRoles.workspaceId),
        eq(workspaces.slug, slug),
      ),
    );

  const perms = defaultUserPermissions();

  for (const permission of results) {
    // @ts-ignore
    perms[permission.resource][permission.action] = true;
  }

  return perms;
}

async function isAdmin(
  { slug }: { slug: string },
  context: Context,
): Promise<boolean> {
  const { tx } = context;
  const user = context.user.unwrap();

  const result = await (tx || db)
    .select({
      admin: sql<boolean>`BOOL_OR(admin)`,
    })
    .from(userWorkspaceRoles)
    .innerJoin(roles, eq(roles.id, userWorkspaceRoles.roleId))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaceRoles.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .where(eq(userWorkspaceRoles.userId, user.id));

  if (!result.length) {
    return false;
  }

  return result[0].admin || false;
}
