import { randomBytes } from 'node:crypto';
import { eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userEmailChanges, users } from '~/.server/db/schema';
import { ApiError, InputError } from '~/.server/errors';
import { createHash, validateHash } from '../auth/hash';
import { sendConfirmEmail } from './send-confirm-email';

type Request = {
  name: string | undefined;
  email: string | undefined;
  password: string | undefined;
  newPassword: string | undefined;
  confirmNewPassword: string | undefined;
};

export async function updateProfile(
  request: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  const name = request.name?.toString().trim();
  const email = request.email?.toString().toLowerCase().trim();

  const { config } = context;

  if (
    !name ||
    name.length < config.USER.MIN_NAME_LENGTH ||
    name.length > config.USER.MAX_NAME_LENGTH
  ) {
    return Err(
      new InputError(
        `Name must be between ${config.USER.MIN_NAME_LENGTH} and ${config.USER.MAX_NAME_LENGTH} characters in length`,
      ),
    );
  }

  if (!email) {
    return Err(new InputError('Email is required'));
  }

  if (!request.password) {
    return Err(new InputError('Password is required'));
  }

  const user = context.user.unwrap();
  if (!user.passwordHash) {
    return Err(new ApiError('Expected user to have password hash'));
  }

  const validPassword = await validateHash(
    `${user.email}${request.password}`,
    user.passwordHash,
  );
  if (!validPassword) {
    return Err(new InputError('Password is incorrect'));
  }

  if (request.newPassword !== request.confirmNewPassword) {
    return Err(new InputError('Passwords do not match'));
  }

  let passwordHash = user.passwordHash;
  if (request.newPassword) {
    passwordHash = await createHash(`${email}${request.newPassword}`);
  }

  await db.transaction(async (tx) => {
    await tx
      .update(users)
      .set({
        name,
        passwordHash,
        updatedBy: user.id,
      })
      .where(eq(users.id, user.id));

    if (email !== user.email) {
      await tx.insert(userEmailChanges).values({
        userId: user.id,
        old: user.email,
        new: email,
        confirmToken: randomBytes(32).toString('hex'),
        undoToken: randomBytes(32).toString('hex'),
        confirmMaxAge: config.USER.CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES * 60,
        undoMaxAge: config.USER.CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES * 60,
        createdBy: user.id,
      });

      await sendConfirmEmail({ ...context, tx });
    }
  });

  return Ok.EMPTY;
}
