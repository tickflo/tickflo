import { useCallback, useMemo, useState } from 'react';
import { FaPlus, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { defaultUserPermissions, isAction } from '~/.server/permissions';
import { addRole } from '~/.server/services/security';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import config from '~/config';
import type { Route } from './+types/workspaces.$slug.roles.add';

export async function action({ context, request, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);

  const formData = await request.formData();
  const name = formData.get('name')?.toString();
  const admin = formData.get('admin') === 'on';
  const users = formData.getAll('users').map((v) => v.toString());
  const roles = formData.getAll('roles').map((v) => v.toString());

  const permissions = defaultUserPermissions();

  for (const action of users) {
    if (!isAction(action)) {
      return data({ error: `Invalid action: ${action}` });
    }

    // @ts-ignore
    permissions.users[action] = true;
  }

  for (const action of roles) {
    if (!isAction(action)) {
      return data({ error: `Invalid action: ${action}` });
    }

    // @ts-ignore
    permissions.roles[action] = true;
  }

  const result = await addRole(
    { slug: params.slug, name, admin, permissions },
    ctx,
  );

  if (result.isErr()) {
    return data({ error: result.error.message });
  }

  return redirect('..');
}

export default function workspaceAddRole({ actionData }: Route.ComponentProps) {
  const errorMessage = actionData?.error;

  const [admin, setAdmin] = useState(false);

  const [checkedUserPermissions, setCheckedUserPermissions] = useState<
    string[]
  >([]);
  const [checkedRolePermissions, setCheckedRolePermissions] = useState<
    string[]
  >([]);

  const permissionTypes = useMemo(
    () => [
      ['create', 'Add'],
      ['read', 'View'],
      ['update', 'Update'],
      ['delete', 'Remove'],
    ],
    [],
  );

  const onUserPermissionsChange = useCallback(
    (name: string, checked: boolean) => {
      if (!checked) {
        setCheckedUserPermissions(
          checkedUserPermissions.filter((n) => n !== name),
        );
      } else {
        setCheckedUserPermissions([...checkedUserPermissions, name]);
      }
    },
    [checkedUserPermissions],
  );

  const onRolePermissionsChange = useCallback(
    (name: string, checked: boolean) => {
      if (!checked) {
        setCheckedRolePermissions(
          checkedRolePermissions.filter((n) => n !== name),
        );
      } else {
        setCheckedRolePermissions([...checkedRolePermissions, name]);
      }
    },
    [checkedRolePermissions],
  );

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Add Role </h3>
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
              maxLength={config.ROLE.MAX_NAME_LENGTH}
            />
          </fieldset>
          <fieldset className="fieldset">
            <label htmlFor="admin" className="fieldset-label">
              Admin
            </label>
            <input
              id="admin"
              name="admin"
              type="checkbox"
              checked={admin}
              onChange={(e) => setAdmin(e.target.checked)}
              className="toggle toggle-primary"
            />
          </fieldset>
          <fieldset
            className="fieldset flex w-full gap-4 rounded-box border border-base-300 p-4"
            disabled={admin}
          >
            <legend className="fieldset-legend">User permissions</legend>
            {permissionTypes.map(([name, label]) => (
              <input
                key={name}
                name="users"
                type="checkbox"
                aria-label={label}
                value={name}
                className="btn"
                checked={admin || checkedUserPermissions.indexOf(name) > -1}
                onChange={(e) =>
                  onUserPermissionsChange(name, e.target.checked)
                }
              />
            ))}
          </fieldset>
          <fieldset
            className="fieldset flex w-full gap-4 rounded-box border border-base-300 p-4"
            disabled={admin}
          >
            <legend className="fieldset-legend">Role permissions</legend>
            {permissionTypes.map(([name, label]) => (
              <input
                key={name}
                name="roles"
                type="checkbox"
                aria-label={label}
                value={name}
                className="btn"
                checked={admin || checkedRolePermissions.indexOf(name) > -1}
                onChange={(e) =>
                  onRolePermissionsChange(name, e.target.checked)
                }
              />
            ))}
          </fieldset>
          {errorMessage && <ErrorAlert message={errorMessage} />}
          <div className="modal-action">
            <button type="submit" form="form-dismiss" className="btn">
              <FaUndo />
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              <FaPlus />
              Add Role
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
