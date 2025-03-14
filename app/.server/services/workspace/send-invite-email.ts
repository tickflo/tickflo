import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { db } from '~/.server/db';
import { userWorkspaces, users, workspaces } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';
import { getEmailTemplateId, sendEmail } from '../email';

type Request = {
  userId: number;
  slug: string;
};

export async function sendInviteEmail(
  { userId, slug }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  const { tx, config } = context;

  const rows = await (tx || db)
    .select({
      accepted: userWorkspaces.accepted,
      name: users.name,
      email: users.email,
      passwordHash: users.passwordHash,
    })
    .from(userWorkspaces)
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaces.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .innerJoin(users, eq(users.id, userWorkspaces.userId))
    .where(eq(userWorkspaces.userId, userId));

  if (!rows || rows.length === 0) {
    return Err(new InputError('User does not belong to workspace'));
  }

  const row = rows[0];

  if (row.accepted) {
    return Err(new InputError('User already accepted invitation'));
  }

  const signedUp = !!row.passwordHash;
  if (signedUp) {
    const templateId = await getEmailTemplateId(
      {
        typeId: emailTemplates.existingWorkspaceMemberInvitation.typeId,
        slug,
      },
      context,
    );

    if (templateId.isNone()) {
      return Err(new InputError('Email template not found'));
    }

    await sendEmail(
      {
        to: row.email,
        templateId: templateId.value,
        vars: {
          name: row.name,
          login_link: `${config.BASE_URL}/workspaces`,
        },
      },
      context,
    );
  } else {
    const templateId = await getEmailTemplateId(
      {
        typeId: emailTemplates.existingWorkspaceMemberInvitation.typeId,
        slug,
      },
      context,
    );

    if (templateId.isNone()) {
      return Err(new InputError('Email template not found'));
    }

    await sendEmail(
      {
        to: row.email,
        templateId: templateId.value,
        vars: {
          name: row.name,
          signup_link: `${config.BASE_URL}/signup?email=${encodeURIComponent(
            row.email,
          )}`,
        },
      },
      context,
    );
  }

  return Ok.EMPTY;
}
