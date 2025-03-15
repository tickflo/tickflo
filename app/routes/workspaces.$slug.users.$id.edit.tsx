import { useCallback, useState } from 'react';
import { FaCheck, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getRoleIdsForUserId, getRoles } from '~/.server/services/security';
import { getUserById, updateUser } from '~/.server/services/user';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import type { Route } from './+types/workspaces.$slug.users.$id.edit';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const roles = await getRoles({ slug: params.slug }, ctx);
  if (roles.isErr()) {
    return errorRedirect(session, roles.error.message, '..');
  }

  const userId = Number.parseInt(params.id, 10);
  const user = await getUserById({ id: userId, slug: params.slug }, ctx);
  if (user.isErr()) {
    return errorRedirect(session, user.error.message, '..');
  }

  const userRoles = await getRoleIdsForUserId(
    { id: user.value.id, slug: params.slug },
    ctx,
  );

  if (userRoles.isErr()) {
    return errorRedirect(session, userRoles.error.message, '..');
  }

  return data({
    user: {
      name: user.value.name,
    },
    userRoles: userRoles.value,
    roles: roles.value.map((r) => ({
      id: r.id,
      name: r.name,
    })),
  });
}

export async function action({ context, request, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);
  const formData = await request.formData();
  const userId = Number.parseInt(params.id, 10);
  const roleIds = formData
    .getAll('roles')
    .map((v) => Number.parseInt(v.toString(), 10));

  const result = await updateUser({ slug: params.slug, userId, roleIds }, ctx);

  if (result.isErr()) {
    return data({ error: result.error.message });
  }

  return redirect('..');
}

export default function workspaceEditUser({
  loaderData,
  actionData,
}: Route.ComponentProps) {
  const { user, roles, userRoles } = loaderData;
  const errorMessage = actionData?.error;

  const [checkedRoles, setCheckedRoles] = useState(userRoles);

  const onChange = useCallback(
    (id: number, checked: boolean) => {
      if (!checked) {
        setCheckedRoles(checkedRoles.filter((r) => r !== id));
      } else {
        setCheckedRoles([...checkedRoles, id]);
      }
    },
    [checkedRoles],
  );

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Edit {user.name} </h3>
        <Form id="form-submit" method="post">
          <fieldset className="fieldset">
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
                  checked={checkedRoles.indexOf(r.id) > -1}
                  onChange={(e) =>
                    onChange(
                      Number.parseInt(e.target.value, 10),
                      e.target.checked,
                    )
                  }
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
              <FaCheck />
              Save Changes
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
