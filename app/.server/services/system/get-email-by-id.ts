import { eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { emails } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { renderTemplate } from '../email';

type Email = {
  id: number;
  subject: string;
  body: string;
  from: string;
  to: string;
  createdAt: Date;
  state: string;
  stateUpdatedAt: Date | null;
  bounceDescription: string | null;
};

export async function getEmailById(
  { id }: { id: number },
  context: Context,
): Promise<Result<Email, ApiError>> {
  const user = context.user.unwrap();

  if (!user.systemAdmin) {
    return Err(new PermissionsError());
  }

  if (Number.isNaN(id)) {
    return Err(new InputError(`Invalid id: ${id}`));
  }

  const email = await db.query.emails.findFirst({
    with: {
      template: true,
    },
    where: eq(emails.id, id),
  });

  if (!email) {
    return Err(new InputError('Email not found'));
  }

  return Ok({
    id: email.id,
    subject: renderTemplate({
      template: email.template.subject,
      vars: email.vars as object,
    }),
    body: renderTemplate({
      template: email.template.body,
      vars: email.vars as object,
    }),
    from: email.from,
    to: email.to,
    createdAt: email.createdAt,
    state: email.state,
    stateUpdatedAt: email.stateUpdatedAt,
    bounceDescription: email.bounceDescription,
  });
}
