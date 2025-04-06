import { FaPlus, FaPuzzlePiece } from 'react-icons/fa';
import { data } from 'react-router';
import { appContext } from '~/app-context';

import { errorRedirect } from '~/.server/helpers';
import { getPortalById } from '~/.server/services/portal/get-portal-by-id';
import type { Route } from './+types/workspaces.$slug.portal-edit.$id';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const portalId = Number.parseInt(params.id, 10);
  const portal = await getPortalById({ slug: params.slug, id: portalId }, ctx);
  if (portal.isErr()) {
    return errorRedirect(session, portal.error.message, '..');
  }

  return data({
    portal: portal.value,
  });
}

export default function workspacePortalEdit({
  loaderData,
}: Route.ComponentProps) {
  const { portal } = loaderData;

  return (
    <>
      <div>
        <h1 className="mb-2 text-2xl">
          <FaPuzzlePiece className="inline pb-1 pl-1" /> Edit Portal -{' '}
          {portal.name}
        </h1>

        <hr className="mb-2" />

        <div className="mb-2 flex w-full justify-end gap-2">
          <button className="btn btn-primary btn-sm" type="button">
            <FaPlus /> Add Section
          </button>
          <button className="btn btn-primary btn-sm" type="button">
            <FaPlus /> Add Question
          </button>
        </div>
      </div>
    </>
  );
}
