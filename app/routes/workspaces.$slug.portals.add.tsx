import { useCallback, useMemo, useState } from 'react';
import { FaPlus, FaUndo } from 'react-icons/fa';
import { Form, data, redirect } from 'react-router';
import { addPortal } from '~/.server/services/portal';
import { appContext } from '~/app-context';
import { ErrorAlert } from '~/components/error-alert';
import config from '~/config';
import { slugify } from '~/utils/slugify';
import type { Route } from './+types/workspaces.$slug.roles.add';

export async function action({ context, request, params }: Route.ActionArgs) {
  const ctx = context.get(appContext);

  const formData = await request.formData();
  const name = formData.get('name')?.toString();

  const result = await addPortal({ slug: params.slug, name }, ctx);

  if (result.isErr()) {
    return data({ error: result.error.message });
  }

  return redirect('..');
}

export default function workspaceAddPortal({
  params,
  actionData,
}: Route.ComponentProps) {
  const errorMessage = actionData?.error;

  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');

  const workspaceSlug = useMemo(() => params.slug, [params.slug]);

  const onNameChange = useCallback((value: string) => {
    setName(value);
    setSlug(slugify(value));
  }, []);

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="font-bold text-lg"> Add Portal </h3>
        <Form id="form-submit" method="post">
          <input type="hidden" name="action" value="add-user" />
          <fieldset className="fieldset">
            <label htmlFor="name" className="fieldset-label">
              Name
            </label>
            {slug.length >= 3 && (
              <em title="Workspace URL Preview">
                {window.location.protocol}
                {'//'}
                {window.location.host}
                /portals/{workspaceSlug}/{slug}
              </em>
            )}
            <input
              id="name"
              name="name"
              type="text"
              className="input w-full"
              placeholder="Name"
              required
              maxLength={config.PORTAL.MAX_NAME_LENGTH}
              value={name}
              onChange={(e) => onNameChange(e.target.value)}
            />
          </fieldset>
          {errorMessage && <ErrorAlert message={errorMessage} />}
          <div className="modal-action">
            <button type="submit" form="form-dismiss" className="btn">
              <FaUndo />
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              <FaPlus />
              Add Portal
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
