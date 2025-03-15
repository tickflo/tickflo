import type { Context } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { sendEmail } from '../email';

type Request = {
  to: string;
  code: string;
};

export async function sendSignupEmail({ to, code }: Request, context: Context) {
  const { config } = context;

  await sendEmail(
    {
      to,
      templateId: emailTemplates.signup.typeId,
      vars: {
        confirmation_link: `${
          config.BASE_URL
        }/email-confirmation/confirm?code=${encodeURIComponent(code)}`,
      },
    },
    context,
  );
}
