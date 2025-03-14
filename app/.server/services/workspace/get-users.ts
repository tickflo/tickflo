import { and, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  roles,
  userWorkspaceRoles,
  users,
  workspaces,
} from '~/.server/db/schema';

type User = {
  id: number;
  name: string;
  email: string;
  role: string;
  inviteAccepted: boolean;
};

export async function getUsers(
  { slug }: { slug: string },
  context: Context,
): Promise<User[]> {
  const { tx } = context;

  return (tx || db)
    .select({
      id: users.id,
      name: users.name,
      email: users.email,
      role: roles.role,
      inviteAccepted: userWorkspaceRoles.accepted,
    })
    .from(users)
    .innerJoin(userWorkspaceRoles, eq(userWorkspaceRoles.userId, users.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaceRoles.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .innerJoin(roles, eq(roles.id, userWorkspaceRoles.roleId));
}
