import { and, eq, sql } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  roles,
  userWorkspaceRoles,
  userWorkspaces,
  users,
  workspaces,
} from '~/.server/db/schema';
import { type ApiError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';

type User = {
  id: number;
  name: string;
  email: string;
  roles: string[];
  inviteAccepted: boolean;
};

export async function getUsers(
  { slug }: { slug: string },
  context: Context,
): Promise<Result<User[], ApiError>> {
  const { tx } = context;

  const permissions = await getPermissions({ slug }, context);
  if (!permissions.users.read) {
    return Err(
      new PermissionsError('You do not have permission to view users'),
    );
  }

  const rows = await (tx || db)
    .select({
      id: users.id,
      name: users.name,
      email: users.email,
      roles: sql<string>`STRING_AGG(roles.name, ', ')`,
      inviteAccepted: userWorkspaces.accepted,
    })
    .from(users)
    .innerJoin(userWorkspaces, eq(userWorkspaces.userId, users.id))
    .innerJoin(userWorkspaceRoles, eq(userWorkspaceRoles.userId, users.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaceRoles.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .innerJoin(roles, eq(roles.id, userWorkspaceRoles.roleId))
    .groupBy(users.id, users.name, users.email, userWorkspaces.accepted);

  return Ok(
    rows.map((r) => ({
      ...r,
      roles: r.roles.split(', '),
    })),
  );
}
