import {
  FaBox,
  FaChartLine,
  FaCheck,
  FaClipboardCheck,
  FaHome,
  FaInbox,
  FaLock,
  FaPuzzlePiece,
  FaUserCircle,
  FaUsers,
  FaWarehouse,
  FaWrench,
} from 'react-icons/fa';
import { FaShield } from 'react-icons/fa6';
import { Link, NavLink, Outlet, data } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getPermissions } from '~/.server/services/security';
import { getUsers } from '~/.server/services/user';
import { getWorkspaceBySlug } from '~/.server/services/workspace';
import { commitSession } from '~/.server/session';
import { appContext } from '~/app-context';
import type { Route } from './+types/workspaces.$slug';

export function meta({ data }: Route.MetaArgs) {
  return [{ title: `Tickflo - ${data.workspace.name}` }];
}

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const workspace = await getWorkspaceBySlug({ slug: params.slug }, ctx);

  if (workspace.isNone()) {
    return errorRedirect(
      session,
      'You do not have access to that workspace',
      '/workspaces',
    );
  }

  const users = await getUsers({ slug: params.slug }, ctx);
  if (users.isErr()) {
    return errorRedirect(session, users.error.message, '/workspaces');
  }

  const permissions = await getPermissions({ slug: params.slug }, ctx);

  return data(
    {
      workspace: {
        slug: workspace.value.slug,
        name: workspace.value.name,
      },
      permissions,
      users: users.value.map((u) => ({
        id: u.id,
        name: u.name,
        tickets: 0,
      })),
    },
    {
      headers: {
        'Set-Cookie': await commitSession(session),
      },
    },
  );
}

export default function workspaces({ loaderData }: Route.ComponentProps) {
  const { workspace, permissions, users } = loaderData;
  const { slug } = workspace;

  return (
    <div className="flex">
      <aside className="sticky top-0 h-screen w-60 overflow-y-auto bg-base-200 px-4 py-6">
        <Link to={`/workspaces/${workspace.slug}`} className="btn btn-ghost">
          {workspace.name}
        </Link>

        <ul className="menu w-56 rounded-box bg-base-200">
          <li>
            <NavLink
              end
              to={`/workspaces/${slug}`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaHome /> Dashboard
            </NavLink>
          </li>

          <li>
            <NavLink
              to={`/workspaces/${slug}/portals`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaPuzzlePiece /> Portals
            </NavLink>
          </li>
          <li>
            <NavLink
              to={`/workspaces/${slug}/contacts`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaUsers /> Contacts
            </NavLink>
          </li>
          <li>
            <details open>
              <summary>
                <FaClipboardCheck /> Tickets
              </summary>
              <ul>
                <li>
                  <Link to={`/workspaces/${slug}/tickets`}>
                    <FaInbox /> Inbox
                    <span className="badge badge-xs badge-primary">10</span>
                  </Link>
                </li>
                <li>
                  <Link to={`/workspaces/${slug}/tickets`}>
                    <FaCheck /> Closed
                  </Link>
                </li>
                {users.map((user) => (
                  <li key={user.id}>
                    <Link to={`/workspaces/${slug}/tickets`}>
                      <FaUserCircle />
                      {user.name}
                      {!!user.tickets && (
                        <span className="badge badge-xs badge-primary">
                          {user.tickets}
                        </span>
                      )}
                    </Link>
                  </li>
                ))}
              </ul>
            </details>
          </li>

          <li>
            <NavLink
              to={`/workspaces/${slug}/reports`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaChartLine /> Reports
            </NavLink>
          </li>

          <li>
            <NavLink
              to={`/workspaces/${slug}/locations`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaWarehouse /> Locations
            </NavLink>
          </li>

          <li>
            <NavLink
              to={`/workspaces/${slug}/inventory`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaBox /> Inventory
            </NavLink>
          </li>

          {(permissions.users.read || permissions.roles.read) && (
            <li>
              <details open>
                <summary>
                  <FaLock /> Security
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
          <li>
            <NavLink
              to={`/workspaces/${slug}/settings`}
              className={({ isActive }) => (isActive ? 'menu-active' : '')}
            >
              <FaWrench /> Settings
            </NavLink>
          </li>
        </ul>
      </aside>
      <div className="flex-1 overflow-x-auto p-2">
        <Outlet />
      </div>
    </div>
  );
}
