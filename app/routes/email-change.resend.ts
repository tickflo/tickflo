import { redirect } from 'react-router';
import { sendConfirmEmail } from '~/.server/services/user';
import { commitSession } from '~/.server/session';
import { appContext } from '~/app-context';
import type { Route } from './+types/email-change.resend';

export async function action({ context, request }: Route.ActionArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const result = await sendConfirmEmail(ctx);
  if (result.isErr()) {
    session.flash('error', result.error.message);
  } else {
    session.flash('message', 'Confirmation email sent');
  }

  return redirect(request.headers.get('referer') || '/', {
    headers: {
      'Set-Cookie': await commitSession(session),
    },
  });
}
