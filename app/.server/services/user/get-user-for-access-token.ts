import { eq } from 'drizzle-orm';
import { None, type Option, Some } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { tokens, type users } from '~/.server/db/schema';

type User = typeof users.$inferSelect;

export async function getUserForAccessToken(
  { token }: { token: string },
  context: Context,
): Promise<Option<User>> {
  const { tx } = context;

  const record = await (tx || db).query.tokens.findFirst({
    where: eq(tokens.token, token),
    with: {
      user: true,
    },
  });

  if (!record) {
    return None;
  }

  const now = new Date();
  const createdAt = new Date(record.createdAt);
  const diff = Math.abs(now.getTime() - createdAt.getTime());
  const seconds = Math.floor(diff / 1000);
  if (seconds > record.maxAge) {
    return None;
  }

  return Some(record.user);
}
