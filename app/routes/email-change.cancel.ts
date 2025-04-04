import { redirect } from 'react-router';
import { cancelEmailChange } from '~/.server/services/user';
import { commitSession } from '~/.server/session';
import { appContext } from '~/app-context';
import type { Route } from './+types/email-change.cancel';

export async function action({ context, request }: Route.ActionArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const result = await cancelEmailChange(ctx);
  if (result.isErr()) {
    session.flash('error', result.error.message);
  }

  return redirect(request.headers.get('referer') || '/', {
    headers: {
      'Set-Cookie': await commitSession(session),
    },
  });
}
