import { FaPlus, FaPuzzlePiece, FaSearch, FaTrash } from 'react-icons/fa';
import { FaBoltLightning, FaPencil } from 'react-icons/fa6';
import { Link, Outlet } from 'react-router';

export default function workspacePortals() {
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
            <tr>
              <td>
                <div className="dropdown">
                  {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                  <div tabIndex={0} role="button" className="btn btn-sm">
                    <FaBoltLightning /> Actions
                  </div>
                  <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                    <li>
                      <Link to="google.com">
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
              <td>Default</td>
              <td>
                <a
                  href="https://app.tickflo.co/portal/cool-guys/default"
                  className="link link-primary"
                >
                  https://app.tickflo.co/portal/cool-guys/default
                </a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <Outlet />
    </>
  );
}
