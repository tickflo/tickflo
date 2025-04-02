import { and, eq, sql } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import config from '~/.server/config';
import type { Context } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { db } from '~/.server/db';
import { userEmailChanges, users } from '~/.server/db/schema';
import { type ApiError, InputError } from '~/.server/errors';
import { prettyDate } from '~/utils/pretty-date';
import { createHash, validateHash } from '../auth/hash';
import { sendEmail } from '../email';
import { getEmailChange } from './get-email-change';

export async function confirmEmailChange(
  {
    code,
    password,
  }: { code: string | undefined; password: string | undefined },
  context: Context,
): Promise<Result<void, ApiError>> {
  if (!code) {
    return Err(new InputError('Invalid code'));
  }

  if (!password) {
    return Err(new InputError('Invalid password'));
  }

  const emailChange = await getEmailChange(context);
  if (emailChange.isNone()) {
    return Err(new InputError('No pending email change for user'));
  }

  if (emailChange.value.confirmToken !== code) {
    return Err(new InputError('Invalid code'));
  }

  const user = context.user.unwrap();
  if (!user.passwordHash) {
    return Err(new InputError('No password set for user'));
  }

  const valid = await validateHash(
    `${user.email}${password}`,
    user.passwordHash,
  );
  if (!valid) {
    return Err(new InputError('Invalid password'));
  }

  const passwordHash = await createHash(`${emailChange.value.new}${password}`);

  await db.transaction(async (tx) => {
    await tx
      .update(userEmailChanges)
      .set({
        confirmedAt: sql`now()`,
      })
      .where(
        and(
          eq(userEmailChanges.userId, emailChange.value.userId),
          eq(userEmailChanges.confirmToken, emailChange.value.confirmToken),
        ),
      );

    await tx
      .update(users)
      .set({
        email: emailChange.value.new,
        passwordHash,
        updatedBy: emailChange.value.userId,
      })
      .where(eq(users.id, emailChange.value.userId));

    const expiresAt = new Date(emailChange.value.createdAt);
    expiresAt.setSeconds(expiresAt.getSeconds() + emailChange.value.undoMaxAge);
    await sendEmail(
      {
        to: emailChange.value.old,
        templateId: emailTemplates.revertEmailChange.typeId,
        vars: {
          new_email: emailChange.value.new,
          expires_at: `${prettyDate(expiresAt)}`,
          revert_link: `${config.BASE_URL}/email-change/undo?id=${emailChange.value.userId}&code=${encodeURIComponent(emailChange.value.undoToken)}`,
        },
      },
      { ...context, tx },
    );
  });

  return Ok.EMPTY;
}
