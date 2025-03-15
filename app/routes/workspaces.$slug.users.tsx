import { FaEnvelope, FaPlus, FaSearch, FaTrash, FaUsers } from 'react-icons/fa';
import { FaBoltLightning, FaPencil, FaPerson, FaShield } from 'react-icons/fa6';
import { Link, Outlet, data, href } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getUsers } from '~/.server/services/user';
import { appContext } from '~/app-context';
import type { Route } from './+types/workspaces.$slug.users';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);

  const users = await getUsers({ slug: params.slug }, ctx);
  if (users.isErr()) {
    return errorRedirect(ctx.session, users.error.message, '..');
  }

  return data({ users: users.value });
}

export default function workspaceUsers({
  loaderData,
  params,
}: Route.ComponentProps) {
  const { users } = loaderData;

  return (
    <>
      <div>
        <h1 className="mb-2 text-2xl">
          <FaUsers className="inline pb-1 pl-1" /> Users
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
              <FaPlus /> Add User
            </Link>
          </div>
        </div>

        <table className="table-zebra table">
          <thead>
            <tr>
              <th />
              <th>
                <FaPerson className="inline pb-1" /> Name
              </th>
              <th>
                <FaEnvelope className="inline pb-1" /> Email
              </th>
              <th>
                <FaShield className="inline pb-1" /> Roles
              </th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>
                  <div className="dropdown">
                    {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                    <div tabIndex={0} role="button" className="btn btn-sm">
                      <FaBoltLightning /> Actions
                    </div>
                    <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                      <li>
                        <Link
                          to={href('/workspaces/:slug/users/:id/edit', {
                            slug: params.slug,
                            id: user.id.toString(),
                          })}
                        >
                          <FaPencil /> Edit
                        </Link>
                      </li>
                      {!user.inviteAccepted && (
                        <li>
                          <Link to="./resend-invite">
                            <FaEnvelope /> Resend Invite
                          </Link>
                        </li>
                      )}
                      <li>
                        <Link
                          to={href('/workspaces/:slug/users/:id/remove', {
                            slug: params.slug,
                            id: user.id.toString(),
                          })}
                          className="text-error"
                        >
                          <FaTrash /> Remove
                        </Link>
                      </li>
                    </ul>
                  </div>
                </td>
                <td> {user.name} </td>
                <td>
                  {user.email}
                  {!user.inviteAccepted && (
                    <div className="badge badge-soft badge-info ml-1">
                      Invited
                    </div>
                  )}
                </td>
                <td className="flex max-w-md flex-wrap gap-2">
                  {user.roles.map((role) => (
                    <div className="badge badge-soft" key={role}>
                      {role}
                    </div>
                  ))}
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
