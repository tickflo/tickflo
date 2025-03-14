import { eq } from 'drizzle-orm';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaceRoles, type workspaces } from '~/.server/db/schema';
import { loginRedirect } from '~/.server/helpers';

type WorkspaceType = typeof workspaces.$inferSelect;

interface Workspace extends WorkspaceType {
  accepted: boolean;
}

export async function getWorkspaces(context: Context): Promise<Workspace[]> {
  const { user, session, tx } = context;

  if (user.isNone()) {
    throw loginRedirect(session);
  }

  const rows = await (tx || db).query.userWorkspaceRoles.findMany({
    where: eq(userWorkspaceRoles.userId, user.value.id),
    with: {
      workspace: true,
    },
  });

  return rows.map((r) => ({
    accepted: r.accepted,
    ...r.workspace,
  }));
}
