import { FaCheck, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getRoleById } from '~/.server/services/security';
import { removeUser } from '~/.server/services/user';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import type { Route } from './+types/workspaces.$slug.roles.$id.remove';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const roleId = Number.parseInt(params.id || '', 10);
  const role = await getRoleById({ id: roleId, slug: params.slug }, ctx);
  if (role.isErr()) {
    return errorRedirect(session, role.error.message, '..');
  }

  return data({
    role: role.value,
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

export default function workspaceRemoveRole({
  actionData,
  loaderData,
}: Route.ComponentProps) {
  const errorMessage = actionData?.error;
  const { role } = loaderData;

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Remove Role </h3>
        <Form id="form-submit" method="post">
          <p>{role.name} will be removed from your workspace.</p>
          {errorMessage && <ErrorAlert message={errorMessage} />}
          <div className="modal-action">
            <button type="submit" form="form-dismiss" className="btn">
              <FaUndo />
              Cancel
            </button>
            <button type="submit" className="btn btn-error">
              <FaCheck />
              Yes, Remove {role.name}
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
