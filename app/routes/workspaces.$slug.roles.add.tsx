import { useCallback, useState } from 'react';
import { FaPlus, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { defaultUserPermissions } from '~/.server/permissions';
import { addRole } from '~/.server/services/security';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import config from '~/config';
import {
  ACTIONS,
  type Action,
  RESOURCES,
  type Resource,
  defaultLoaderPermissions,
  isAction,
} from '~/permissions';
import type { Route } from './+types/workspaces.$slug.roles.add';

export async function action({ context, request, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);

  const formData = await request.formData();
  const name = formData.get('name')?.toString();
  const admin = formData.get('admin') === 'on';

  const permissions = defaultUserPermissions();

  for (const { key } of RESOURCES) {
    const values = formData.getAll(key).map((v) => v.toString());
    for (const action of values) {
      if (!isAction(action)) {
        return data({ error: `Invalid action: ${action}` });
      }

      // @ts-ignore
      permissions[key][action] = true;
    }
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

  const [checkedPermissions, setCheckedPermissions] = useState(
    defaultLoaderPermissions,
  );

  const onPermissionsChange = useCallback(
    (resource: Resource, action: Action, checked: boolean) => {
      const newPermissions = JSON.parse(JSON.stringify(checkedPermissions));
      if (!checked) {
        newPermissions[resource] = checkedPermissions[resource].filter(
          (a) => a !== action,
        );
      } else {
        newPermissions[resource].push(action);
      }

      setCheckedPermissions(newPermissions);
    },
    [checkedPermissions],
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
          {RESOURCES.map(({ key, label }) => (
            <fieldset
              className="fieldset flex w-full gap-4 rounded-box border border-base-300 p-4"
              disabled={admin}
              key={key}
            >
              <legend className="fieldset-legend">{label} permissions</legend>
              {ACTIONS.map(({ action, label }) => (
                <input
                  key={action}
                  name={key}
                  type="checkbox"
                  aria-label={label}
                  value={action}
                  className="btn"
                  checked={
                    admin || checkedPermissions[key].indexOf(action) > -1
                  }
                  onChange={(e) =>
                    onPermissionsChange(key, action, e.target.checked)
                  }
                />
              ))}
            </fieldset>
          ))}
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
