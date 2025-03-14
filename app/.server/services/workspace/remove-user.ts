import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { db } from '~/.server/db';
import { userWorkspaceRoles, users } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { getEmailTemplateId, sendEmail } from '../email';
import { getUserCount } from './get-user-count';
import { getWorkspaceBySlug } from './get-workspace-by-slug';

type Request = {
  userId: number;
  slug: string;
};

export async function removeUser(
  { userId, slug }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  if (Number.isNaN(userId)) {
    return Err(new InputError('Invalid user id'));
  }

  const { tx, permissions } = context;

  if (!permissions.users.delete) {
    return Err(
      new PermissionsError('You do not have permission to remove users'),
    );
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);

  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  const userCount = await getUserCount({ slug }, context);

  if (userCount <= 1) {
    return Err(new InputError('Can not remove last member of workspace'));
  }

  const rows = await (tx || db)
    .select({ id: users.id, email: users.email, name: users.name })
    .from(userWorkspaceRoles)
    .innerJoin(
      users,
      and(eq(users.id, userId), eq(users.id, userWorkspaceRoles.userId)),
    )
    .where(eq(userWorkspaceRoles.workspaceId, workspace.value.id));

  if (rows.length === 0) {
    return Err(new InputError('User is not a member of this workspace'));
  }

  const removeUser = rows[0];

  await db.transaction(async (tx) => {
    await tx
      .delete(userWorkspaceRoles)
      .where(
        and(
          eq(userWorkspaceRoles.userId, userId),
          eq(userWorkspaceRoles.workspaceId, workspace.value.id),
        ),
      );

    const templateId = await getEmailTemplateId(
      {
        slug,
        typeId: emailTemplates.workspaceMemberRemoval.typeId,
      },
      { ...context, tx },
    );

    if (templateId.isNone()) {
      return Err(new InputError('Email template not found'));
    }

    await sendEmail(
      {
        to: removeUser.email,
        templateId: templateId.value,
        vars: {
          name: removeUser.name,
          workspace_name: workspace.value.name,
        },
      },
      { ...context, tx },
    );
  });

  return Ok.EMPTY;
}
