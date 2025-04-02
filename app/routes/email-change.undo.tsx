import { Form, data, redirect } from 'react-router';
import { getContext } from '~/.server/context';
import { errorRedirect } from '~/.server/helpers';
import { undoEmailChange } from '~/.server/services/user';
import { commitSession } from '~/.server/session';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import type { Route } from './+types/email-change.undo';

export async function loader({ context, request }: Route.LoaderArgs) {
  const url = new URL(request.url);
  const code = url.searchParams.get('code');
  const userId = url.searchParams.get('id');
  const ctx = context.get(appContext);
  const { session } = ctx;

  if (!code) {
    return errorRedirect(session, 'Invalid code');
  }

  if (!userId) {
    return errorRedirect(session, 'Invalid id');
  }

  return data({
    userId,
    code,
  });
}

export async function action({ request }: Route.ActionArgs) {
  const formData = await request.formData();
  const password = formData.get('password')?.toString();
  const code = formData.get('code')?.toString();
  const userId = Number.parseInt(formData.get('user-id')?.toString() || '', 10);

  const context = await getContext(request);
  const { session } = context;

  const result = await undoEmailChange({ code, password, userId }, context);
  if (result.isErr()) {
    return data({
      error: result.error.message,
    });
  }

  session.flash('message', 'Email address changed!');

  return redirect('/', {
    headers: {
      'Set-Cookie': await commitSession(session),
    },
  });
}

export default function UndoChangeEmail({
  loaderData,
  actionData,
}: Route.ComponentProps) {
  const { code, userId } = loaderData;

  return (
    <div className="flex min-h-screen flex-col items-center bg-base-200 pt-4">
      <div className="card w-full max-w-xl flex-shrink-0 bg-base-100 shadow-2xl">
        <div className="card-body">
          <h2 className="card-title">Undo email change</h2>
          <span>Your password is required to continue with this change</span>
          <Form method="post">
            <input type="hidden" name="user-id" value={userId} />
            <input type="hidden" name="code" value={code} />
            <fieldset className="fieldset">
              <label htmlFor="password" className="fieldset-label">
                Password
              </label>
              <input
                id="password"
                name="password"
                type="password"
                className="input"
                placeholder="Password"
              />
            </fieldset>

            {actionData?.error && <ErrorAlert message={actionData.error} />}

            <button type="submit" className="btn btn-primary mt-4">
              Undo email change
            </button>
          </Form>
        </div>
      </div>
    </div>
  );
}
