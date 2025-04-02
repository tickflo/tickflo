import { and, desc, eq, isNull } from 'drizzle-orm';
import { None, type Option, Some } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userEmailChanges } from '~/.server/db/schema';

type EmailChange = typeof userEmailChanges.$inferSelect;

export async function getEmailChange(
  context: Context,
): Promise<Option<EmailChange>> {
  const user = context.user.unwrap();
  const { tx } = context;

  const result = await (tx || db).query.userEmailChanges.findFirst({
    where: and(
      eq(userEmailChanges.userId, user.id),
      isNull(userEmailChanges.confirmedAt),
    ),
    orderBy: desc(userEmailChanges.createdAt),
  });

  if (!result) {
    return None;
  }

  return Some(result);
}
