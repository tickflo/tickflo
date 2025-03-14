import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaceRoles, userWorkspaces } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';

type Request = {
  workspaceId: number;
};

export async function declineWorkspaceInvite(
  { workspaceId }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  const { tx } = context;

  const user = context.user.unwrap();

  if (Number.isNaN(workspaceId)) {
    return Err(new InputError(`Invalid workspaceId ${workspaceId}`));
  }

  const workspace = await (tx || db).query.userWorkspaces.findFirst({
    columns: {
      accepted: true,
    },
    where: and(
      eq(userWorkspaceRoles.userId, user.id),
      eq(userWorkspaceRoles.workspaceId, workspaceId),
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

  await db.transaction(async (tx) => {
    await tx
      .delete(userWorkspaces)
      .where(
        and(
          eq(userWorkspaces.userId, user.id),
          eq(userWorkspaces.workspaceId, workspaceId),
        ),
      );

    await tx
      .delete(userWorkspaceRoles)
      .where(
        and(
          eq(userWorkspaceRoles.userId, user.id),
          eq(userWorkspaceRoles.workspaceId, workspaceId),
        ),
      );
  });

  return Ok.EMPTY;
}
