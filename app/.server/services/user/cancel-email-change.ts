import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userEmailChanges } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';
import { getEmailChange } from './get-email-change';

export async function cancelEmailChange(
  context: Context,
): Promise<Result<void, ApiError>> {
  const emailChange = await getEmailChange(context);
  if (emailChange.isNone()) {
    return Err(new InputError('No pending email change for user'));
  }

  await db
    .update(userEmailChanges)
    .set({
      confirmMaxAge: 0,
      undoMaxAge: 0,
    })
    .where(
      and(
        eq(userEmailChanges.userId, emailChange.value.userId),
        eq(userEmailChanges.confirmToken, emailChange.value.confirmToken),
      ),
    );

  return Ok.EMPTY;
}
