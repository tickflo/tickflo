import { and, count, eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaces, users, workspaces } from '~/.server/db/schema';

export async function getUserCount(
  { slug }: { slug: string },
  context: Context,
): Promise<number> {
  const { tx } = context;

  const result = await (tx || db)
    .select({
      count: count(),
    })
    .from(users)
    .innerJoin(userWorkspaces, eq(userWorkspaces.userId, users.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaces.workspaceId),
        eq(workspaces.slug, slug),
      ),
    );

  if (result.length === 0) {
    return 0;
  }

  return result[0].count;
}
