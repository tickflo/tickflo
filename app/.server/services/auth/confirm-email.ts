import { eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { users } from '~/.server/db/schema';
import { InputError } from '~/.server/errors';

type Request = {
  code: string | null | undefined;
};

export async function confirmEmail(
  { code }: Request,
  context: Context,
): Promise<Result<void, InputError>> {
  if (!code) {
    return Err(new InputError('Confirmation code is required'));
  }

  const { tx } = context;

  const user = context.user.unwrap();

  if (user.emailConfirmed || user.emailConfirmationCode !== code) {
    return Err(new InputError('Invalid email or confirmation code'));
  }

  await (tx || db)
    .update(users)
    .set({
      emailConfirmed: true,
      emailConfirmationCode: null,
    })
    .where(eq(users.id, user.id));

  return Ok.EMPTY;
}
