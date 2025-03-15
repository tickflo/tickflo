import { FaPlus, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { AuthError } from '~/.server/errors';
import { errorRedirect } from '~/.server/helpers';
import { getRoles } from '~/.server/services/security';
import { addUser } from '~/.server/services/user';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import config from '~/config';
import type { Route } from './+types/workspaces.$slug.users.add';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { user, session } = ctx;

  if (user.isNone()) {
    throw new AuthError('User not found');
  }

  const roles = await getRoles({ slug: params.slug }, ctx);
  if (roles.isErr()) {
    return errorRedirect(session, roles.error.message, '..');
  }

  return data({
    roles: roles.value,
  });
}

export async function action({ context, request, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);
  const { user } = ctx;

  if (user.isNone()) {
    throw new AuthError('User not found');
  }

  const formData = await request.formData();
  const name = formData.get('name')?.toString();
  const email = formData.get('email')?.toString();
  const roleIds = formData
    .getAll('roles')
    .map((v) => Number.parseInt(v.toString(), 10));

  const result = await addUser(
    { slug: params.slug, name, email, roleIds },
    ctx,
  );

  if (result.isErr()) {
    return data({ error: result.error.message });
  }

  return redirect('..');
}

export default function workspaceAddUser({
  loaderData,
  actionData,
}: Route.ComponentProps) {
  const { roles } = loaderData;
  const errorMessage = actionData?.error;

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Add User </h3>
        <Form id="form-submit" method="post">
          <input type="hidden" name="action" value="add-user" />
          <fieldset className="fieldset">
            <label htmlFor="name" className="fieldset-label">
              Name
            </label>
            <input
              id="name"
              name="name"
              type="text"
              className="input w-full"
              placeholder="Name"
              required
              maxLength={config.USER.MAX_NAME_LENGTH}
            />
            <label htmlFor="email" className="fieldset-label">
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              className="input w-full"
              placeholder="Email"
              required
            />
            <label htmlFor="roles" className="fieldset-label">
              Roles
            </label>
            <div className="flex max-w-full flex-wrap gap-3">
              {roles.map((r) => (
                <input
                  type="checkbox"
                  className="btn"
                  key={r.id}
                  aria-label={r.name}
                  value={r.id}
                  name="roles"
                />
              ))}
            </div>
            {errorMessage && <ErrorAlert message={errorMessage} />}
          </fieldset>
          <div className="modal-action">
            <button type="submit" form="form-dismiss" className="btn">
              <FaUndo />
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              <FaPlus />
              Add User
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
