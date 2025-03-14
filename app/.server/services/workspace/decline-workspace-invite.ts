import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaceRoles } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';
import { loginRedirect } from '~/.server/helpers';

type Request = {
  workspaceId: number;
};

export async function declineWorkspaceInvite(
  { workspaceId }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  const { user, session, tx } = context;

  if (user.isNone()) {
    throw loginRedirect(session);
  }

  if (Number.isNaN(workspaceId)) {
    return Err(new InputError(`Invalid workspaceId ${workspaceId}`));
  }

  const role = await (tx || db).query.userWorkspaceRoles.findFirst({
    columns: {
      accepted: true,
    },
    where: and(
      eq(userWorkspaceRoles.userId, user.value.id),
      eq(userWorkspaceRoles.workspaceId, workspaceId),
    ),
  });

  if (!role) {
    return Err(
      new InputError('Could not find pending invite for that workspace'),
    );
  }

  if (role.accepted) {
    return Err(new InputError('Invite already accepted'));
  }

  await db
    .delete(userWorkspaceRoles)
    .where(
      and(
        eq(userWorkspaceRoles.userId, userId),
        eq(userWorkspaceRoles.workspaceId, workspaceId),
      ),
    );

  return Ok.EMPTY;
}
