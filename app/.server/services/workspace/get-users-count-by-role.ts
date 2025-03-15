import { and, count, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles, userWorkspaceRoles, workspaces } from '~/.server/db/schema';

type RoleGroup = {
  admin: boolean;
  name: string;
  count: number;
};

export async function getUsersCountByRole(
  { slug }: { slug: string },
  context: Context,
): Promise<RoleGroup[]> {
  const { tx } = context;

  const results = await (tx || db)
    .select({
      admin: roles.admin,
      name: roles.name,
      count: count(userWorkspaceRoles.userId),
    })
    .from(roles)
    .innerJoin(userWorkspaceRoles, eq(userWorkspaceRoles.roleId, roles.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaceRoles.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .groupBy(roles.admin, roles.name);

  return results;
}
