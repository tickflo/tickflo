import { redirect } from 'react-router';
import { destroySession } from '~/.server/session';
import { appContext } from '~/app-context';
import type { Route } from './+types/_index';

export async function loader({ context }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  if (session.get('userId')) {
    return redirect('/workspaces');
  }

  return redirect('/login', {
    headers: {
      'Set-Cookie': await destroySession(session),
    },
  });
}
