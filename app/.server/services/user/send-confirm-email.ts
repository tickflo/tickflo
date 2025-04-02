import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { type ApiError, InputError } from '~/.server/errors';
import { sendEmail } from '../email';
import { getEmailChange } from './get-email-change';

export async function sendConfirmEmail(
  context: Context,
): Promise<Result<void, ApiError>> {
  const emailChange = await getEmailChange(context);
  if (emailChange.isNone()) {
    return Err(new InputError('No pending email change for user'));
  }

  const { config } = context;

  await sendEmail(
    {
      to: emailChange.value.new,
      templateId: emailTemplates.confirmNewEmail.typeId,
      vars: {
        confirmation_link: `${
          config.BASE_URL
        }/email-change/confirm?code=${encodeURIComponent(emailChange.value.confirmToken)}`,
      },
    },
    context,
  );

  return Ok.EMPTY;
}
