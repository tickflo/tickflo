import { FaCheck, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getUserById, removeUser } from '~/.server/services/user';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import type { Route } from './+types/workspaces.$slug.users.$id.remove';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const removeUserId = Number.parseInt(params.id || '', 10);
  const removeUser = await getUserById(
    { id: removeUserId, slug: params.slug },
    ctx,
  );
  if (removeUser.isErr()) {
    return errorRedirect(session, removeUser.error.message, '..');
  }

  return data({
    removeUser: removeUser.value,
  });
}

export async function action({ context, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);

  const userId = Number.parseInt(params.id || '', 10);

  const result = await removeUser({ userId, slug: params.slug }, ctx);
  if (result.isErr()) {
    return data({ error: result.error.message });
  }

  return redirect('..');
}

export default function workspaceRemoveUser({
  actionData,
  loaderData,
}: Route.ComponentProps) {
  const errorMessage = actionData?.error;
  const { removeUser } = loaderData;

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Remove User </h3>
        <Form id="form-submit" method="post">
          <p>
            {removeUser.name} will be removed from your workspace. All tickets
            assigned to them will be unassigned.
          </p>
          {errorMessage && <ErrorAlert message={errorMessage} />}
          <div className="modal-action">
            <button type="submit" form="form-dismiss" className="btn">
              <FaUndo />
              Cancel
            </button>
            <button type="submit" className="btn btn-error">
              <FaCheck />
              Yes, Remove {removeUser.name}
            </button>
          </div>
        </Form>
      </div>
      <Form
        id="form-dismiss"
        action=".."
        method="get"
        className="modal-backdrop backdrop-brightness-50"
      >
        <button type="submit"> Close </button>
      </Form>
    </dialog>
  );
}
