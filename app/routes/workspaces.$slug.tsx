import { FaLock, FaUsers } from 'react-icons/fa';
import { FaShield } from 'react-icons/fa6';
import { Link, NavLink, Outlet, data } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getWorkspaceBySlug } from '~/.server/services/workspace';
import { commitSession } from '~/.server/session';
import { appContext } from '~/app-context';
import type { Route } from './+types/workspaces.$slug';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session, permissions } = ctx;

  const workspace = await getWorkspaceBySlug({ slug: params.slug }, ctx);

  if (workspace.isNone()) {
    return errorRedirect(
      session,
      'You do not have access to that workspace',
      '/workspaces',
    );
  }

  return data(
    { workspace: workspace.value, permissions },
    {
      headers: {
        'Set-Cookie': await commitSession(session),
      },
    },
  );
}

export default function workspaces({ loaderData }: Route.ComponentProps) {
  const { workspace, permissions } = loaderData;
  const { slug } = workspace;

  return (
    <div className="flex">
      <aside className="sticky top-0 h-screen w-60 overflow-y-auto bg-base-200 px-4 py-6">
        <Link to={`/workspaces/${workspace.slug}`} className="btn btn-ghost">
          {workspace.name}
        </Link>

        <ul className="menu w-56 rounded-box bg-base-200">
          {(permissions.users.read || permissions.roles.read) && (
            <li>
              <details open>
                <summary>
                  <FaLock /> Authorization
                </summary>
                <ul>
                  {permissions.users.read && (
                    <li>
                      <NavLink
                        to={`/workspaces/${slug}/users`}
                        className={({ isActive }) =>
                          isActive ? 'menu-active' : ''
                        }
                      >
                        <FaUsers /> Users
                      </NavLink>
                    </li>
                  )}
                  {permissions.roles.read && (
                    <li>
                      <NavLink
                        to={`/workspaces/${workspace.slug}/roles`}
                        className={({ isActive }) =>
                          isActive ? 'menu-active' : ''
                        }
                      >
                        <FaShield /> Roles
                      </NavLink>
                    </li>
                  )}
                </ul>
              </details>
            </li>
          )}
        </ul>
      </aside>
      <div className="flex-1 overflow-x-auto p-2">
        <Outlet />
      </div>
    </div>
  );
}
