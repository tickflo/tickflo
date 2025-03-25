import { desc } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { emails } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { renderTemplate } from '../email';

type Email = {
  id: number;
  subject: string;
  from: string;
  to: string;
  createdAt: Date;
  state: string;
  stateUpdatedAt: Date | null;
  bounceDescription: string | null;
};

const PAGE_SIZE = 25;

export async function getEmails(
  { page }: { page: number },
  context: Context,
): Promise<Result<Email[], ApiError>> {
  const user = context.user.unwrap();

  if (!user.systemAdmin) {
    return Err(new PermissionsError());
  }

  if (Number.isNaN(page) || page < 1) {
    return Err(new InputError(`Invalid page number: ${page}`));
  }

  const rows = await db.query.emails.findMany({
    with: {
      template: true,
    },
    orderBy: desc(emails.createdAt),
    limit: PAGE_SIZE,
    offset: (page - 1) * PAGE_SIZE,
  });

  return Ok(
    rows.map((e) => ({
      id: e.id,
      subject: renderTemplate({
        template: e.template.subject,
        vars: e.vars as object,
      }),
      from: e.from,
      to: e.to,
      createdAt: e.createdAt,
      state: e.state,
      stateUpdatedAt: e.stateUpdatedAt,
      bounceDescription: e.bounceDescription,
    })),
  );
}
