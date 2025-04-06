import { FaPlus, FaPuzzlePiece, FaSearch, FaTrash } from 'react-icons/fa';
import { FaBoltLightning, FaPencil } from 'react-icons/fa6';
import { Link, Outlet, data, href } from 'react-router';
import { appContext } from '~/app-context';

import config from '~/.server/config';
import { errorRedirect } from '~/.server/helpers';
import { getPortals } from '~/.server/services/portal';
import type { Route } from './+types/workspaces.$slug.portals';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const portals = await getPortals({ slug: params.slug }, ctx);
  if (portals.isErr()) {
    return errorRedirect(session, portals.error.message, '..');
  }

  return data({
    portals: portals.value.map((r) => ({
      id: r.id,
      name: r.name,
      url: `${config.BASE_URL}/portals/${params.slug}/${r.slug}`,
    })),
  });
}
export default function workspacePortals({
  params,
  loaderData,
}: Route.ComponentProps) {
  const { portals } = loaderData;

  return (
    <>
      <div>
        <h1 className="mb-2 text-2xl">
          <FaPuzzlePiece className="inline pb-1 pl-1" /> Portals
        </h1>

        <hr className="mb-2" />

        <div className="mb-2 flex w-full items-center justify-between">
          <div>
            <label className="input input-bordered flex items-center gap-2">
              <input type="text" className="grow" placeholder="Search" />
              <FaSearch />
            </label>
          </div>
          <div>
            <Link to="./add" className="btn btn-primary btn-sm">
              <FaPlus /> Add Portal
            </Link>
          </div>
        </div>

        <table className="table-zebra table">
          <thead>
            <tr>
              <th />
              <th>Name</th>
              <th>URL</th>
            </tr>
          </thead>
          <tbody>
            {portals.map((portal) => (
              <tr key={portal.id}>
                <td>
                  <div className="dropdown">
                    {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                    <div tabIndex={0} role="button" className="btn btn-sm">
                      <FaBoltLightning /> Actions
                    </div>
                    <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                      <li>
                        <Link
                          to={href('/workspaces/:slug/portal-edit/:id', {
                            slug: params.slug,
                            id: portal.id.toString(),
                          })}
                        >
                          <FaPencil /> Edit
                        </Link>
                      </li>
                      <li>
                        <Link to="google.com" className="text-error">
                          <FaTrash /> Remove
                        </Link>
                      </li>
                    </ul>
                  </div>
                </td>
                <td>{portal.name}</td>
                <td>
                  <a href={portal.url} className="link link-primary">
                    {portal.url}
                  </a>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Outlet />
    </>
  );
}
