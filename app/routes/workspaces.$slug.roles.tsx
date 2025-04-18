import { FaPlus, FaSearch, FaTrash, FaUsers } from 'react-icons/fa';
import { FaBoltLightning, FaPencil, FaShield } from 'react-icons/fa6';
import { Link, Outlet, data, href } from 'react-router';
import { AuthError } from '~/.server/errors';
import { errorRedirect } from '~/.server/helpers';
import { getRoles } from '~/.server/services/security';
import { getUsersCountByRole } from '~/.server/services/user';
import { appContext } from '~/app-context';
import type { Route } from './+types/workspaces.$slug.roles';

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

  const roleGroups = await getUsersCountByRole({ slug: params.slug }, ctx);

  return data({
    roles: roles.value.map((r) => ({
      id: r.id,
      name: r.name,
    })),
    roleGroups,
  });
}

export default function workspaceRoles({
  loaderData,
  params,
}: Route.ComponentProps) {
  const { roles, roleGroups } = loaderData;

  return (
    <>
      <div>
        <h1 className="mb-2 text-2xl">
          <FaShield className="inline pb-1 pl-1" /> Roles
        </h1>

        <hr className="mb-2" />

        <div className="flex w-full items-center justify-between">
          <div>
            <label className="input input-bordered flex items-center gap-2">
              <input type="text" className="grow" placeholder="Search" />
              <FaSearch />
            </label>
          </div>
          <div>
            <Link to="./add" className="btn btn-primary btn-sm">
              <FaPlus /> Add Role
            </Link>
          </div>
        </div>

        <table className="table-zebra table">
          <thead>
            <tr>
              <th />
              <th>
                <FaShield className="inline pb-1" /> Name
              </th>
              <th>
                <FaUsers className="inline pb-1" /> Users
              </th>
            </tr>
          </thead>
          <tbody>
            {roles.map((role) => (
              <tr key={role.name}>
                <td>
                  <div className="dropdown">
                    {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                    <div tabIndex={0} role="button" className="btn btn-sm">
                      <FaBoltLightning /> Actions
                    </div>
                    <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                      <li>
                        <Link
                          to={href('/workspaces/:slug/roles/:id/edit', {
                            slug: params.slug,
                            id: role.id.toString(),
                          })}
                        >
                          <FaPencil /> Edit
                        </Link>
                      </li>
                      <li>
                        <Link
                          to={href('/workspaces/:slug/roles/:id/remove', {
                            slug: params.slug,
                            id: role.id.toString(),
                          })}
                          className="text-error"
                        >
                          <FaTrash /> Remove
                        </Link>
                      </li>
                    </ul>
                  </div>
                </td>
                <td>{role.name}</td>
                <td>
                  {roleGroups.find((g) => g.name === role.name)?.count || '-'}
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
