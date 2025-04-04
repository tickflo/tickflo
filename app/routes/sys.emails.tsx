import { useCallback } from 'react';
import { FaEnvelope, FaEnvelopeOpenText, FaSearch } from 'react-icons/fa';
import { Link, Outlet, data, href, useSearchParams } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getEmails } from '~/.server/services/system';
import { appContext } from '~/app-context';
import { capitalize } from '~/utils/capitalize';
import { prettyDate } from '~/utils/pretty-date';
import type { Route } from './+types/sys.emails';

export function meta() {
  return [{ title: 'Tickflo - Email Log' }];
}

export async function loader({ request, context }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const url = new URL(request.url);
  const page = Number.parseInt(url.searchParams.get('page') || '1', 10);
  const emails = await getEmails({ page }, ctx);

  if (emails.isErr()) {
    return errorRedirect(ctx.session, emails.error.message, '/workspaces');
  }

  return data({
    emails: emails.value,
  });
}

export default function emails({ loaderData }: Route.ComponentProps) {
  const { emails } = loaderData;
  const [searchParams] = useSearchParams();
  const page = Number.parseInt(searchParams.get('page') || '1', 10);

  const getStatusColor = useCallback((state: string) => {
    switch (state) {
      case 'created':
        return 'neutral';
      case 'delivered':
        return 'success';
      case 'bounced':
        return 'error';
      case 'sent':
        return 'primary';
      default:
        throw new Error(`Unhandled state: ${state}`);
    }
  }, []);

  return (
    <>
      <div className="p-10">
        <h1 className="mb-2 text-2xl">
          <FaEnvelope className="inline pb-1 pl-1" /> Emails
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
            <div className="join">
              <Link
                to={`/sys/emails?page=${Math.max(page - 1, 1)}`}
                type="button"
                className="join-item btn"
              >
                «
              </Link>
              <button type="button" className="join-item btn">
                Page {page}
              </button>
              <Link
                to={`/sys/emails?page=${page + 1}`}
                type="button"
                className="join-item btn"
              >
                »
              </Link>
            </div>
          </div>
        </div>

        <table className="table-zebra table">
          <thead>
            <tr>
              <th />
              <th>To</th>
              <th>Subject</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {emails.map((e) => (
              <tr key={e.id}>
                <td>
                  <Link
                    to={href('/sys/emails/:id/preview', {
                      id: e.id.toString(),
                    })}
                    className="btn btn-outline btn-primary"
                  >
                    <FaEnvelopeOpenText /> Preview
                  </Link>
                </td>
                <td>{e.to}</td>
                <td>{e.subject}</td>
                <td>
                  <span
                    className={`status status-${getStatusColor(e.state)} mx-3`}
                  />
                  <span
                    className={e.state === 'bounced' ? 'tooltip' : ''}
                    data-tip={e.bounceDescription || ''}
                  >
                    {capitalize(e.state)} at{' '}
                    {prettyDate(e.stateUpdatedAt || e.createdAt)}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <Outlet />
      </div>
    </>
  );
}
