import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaces } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';

type Request = {
  userId: number;
  workspaceId: number;
};

export async function acceptWorkspaceInvite(
  { userId, workspaceId }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  if (Number.isNaN(userId)) {
    return Err(new InputError(`Invalid userId ${userId}`));
  }

  if (Number.isNaN(workspaceId)) {
    return Err(new InputError(`Invalid workspaceId ${workspaceId}`));
  }

  const { tx } = context;

  const workspace = await (tx || db).query.userWorkspaces.findFirst({
    columns: {
      accepted: true,
    },
    where: and(
      eq(userWorkspaces.userId, userId),
      eq(userWorkspaces.workspaceId, workspaceId),
    ),
  });

  if (!workspace) {
    return Err(
      new InputError('Could not find pending invite for that workspace'),
    );
  }

  if (workspace.accepted) {
    return Err(new InputError('Invite already accepted'));
  }

  await db
    .update(userWorkspaces)
    .set({ accepted: true })
    .where(
      and(
        eq(userWorkspaces.userId, userId),
        eq(userWorkspaces.workspaceId, workspaceId),
      ),
    );

  return Ok.EMPTY;
}
