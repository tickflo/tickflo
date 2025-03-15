import { eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaces, type workspaces } from '~/.server/db/schema';

type WorkspaceType = typeof workspaces.$inferSelect;

interface Workspace extends WorkspaceType {
  accepted: boolean;
}

export async function getWorkspaces(context: Context): Promise<Workspace[]> {
  const { tx } = context;

  const user = context.user.unwrap();

  const rows = await (tx || db).query.userWorkspaces.findMany({
    where: eq(userWorkspaces.userId, user.id),
    with: {
      workspace: true,
    },
  });

  return rows.map((r) => ({
    accepted: r.accepted,
    ...r.workspace,
  }));
}
