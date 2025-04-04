import { and, eq, sql } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userEmailChanges, users } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';
import { createHash, validateHash } from '../auth/hash';

export async function undoEmailChange(
  {
    code,
    password,
    userId,
  }: { code: string | undefined; password: string | undefined; userId: number },
  _context: Context,
): Promise<Result<void, ApiError>> {
  if (!code) {
    return Err(new InputError('Invalid code'));
  }

  if (!password) {
    return Err(new InputError('Invalid password'));
  }

  if (Number.isNaN(userId)) {
    return Err(new InputError('Invalid id'));
  }

  const emailChange = await db.query.userEmailChanges.findFirst({
    where: and(
      eq(userEmailChanges.userId, userId),
      eq(userEmailChanges.undoToken, code),
    ),
  });

  if (!emailChange) {
    return Err(new InputError('Invalid code'));
  }

  const user = await db.query.users.findFirst({
    where: eq(users.id, userId),
  });

  if (!user?.passwordHash) {
    return Err(new InputError('No password set for user'));
  }

  const valid = await validateHash(
    `${user.email}${password}`,
    user.passwordHash,
  );

  if (!valid) {
    return Err(new InputError('Invalid password'));
  }

  const passwordHash = await createHash(`${emailChange.old}${password}`);

  await db.transaction(async (tx) => {
    await tx
      .update(userEmailChanges)
      .set({
        undoneAt: sql`now()`,
      })
      .where(
        and(
          eq(userEmailChanges.userId, emailChange.userId),
          eq(userEmailChanges.undoToken, emailChange.undoToken),
        ),
      );

    await tx
      .update(users)
      .set({
        email: emailChange.old,
        passwordHash,
        updatedBy: emailChange.userId,
      })
      .where(eq(users.id, emailChange.userId));
  });

  return Ok.EMPTY;
}
