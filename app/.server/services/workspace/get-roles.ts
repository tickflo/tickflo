import { and, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { loginRedirect } from '~/.server/helpers';
import { roles, type roles as rolesType, workspaces } from '../../db/schema';

type Role = typeof rolesType.$inferSelect;

export async function getRoles(
  { slug }: { slug: string },
  context: Context,
): Promise<Role[]> {
  const { user, session, tx } = context;

  if (user.isNone()) {
    throw loginRedirect(session);
  }

  return (tx || db)
    .select({
      id: roles.id,
      workspaceId: roles.workspaceId,
      role: roles.role,
      createdAt: roles.createdAt,
      createdBy: roles.createdBy,
      updatedAt: roles.updatedAt,
      updatedBy: roles.updatedBy,
    })
    .from(roles)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, roles.workspaceId), eq(workspaces.slug, slug)),
    );
}
