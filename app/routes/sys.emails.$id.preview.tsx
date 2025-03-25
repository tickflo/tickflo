import { useCallback } from 'react';
import { FaUndo } from 'react-icons/fa';
import { Form, data } from 'react-router';
import { errorRedirect } from '~/.server/helpers';
import { getEmailById } from '~/.server/services/system';
import { appContext } from '~/app-context';
import { capitalize } from '~/utils/capitalize';
import { prettyDate } from '~/utils/pretty-date';
import type { Route } from './+types/sys.emails.$id.preview';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const id = Number.parseInt(params.id, 10);
  const email = await getEmailById({ id }, ctx);
  if (email.isErr()) {
    return errorRedirect(ctx.session, email.error.message, '/workspaces');
  }

  return data({
    email: email.value,
  });
}

export default function emailPreview({ loaderData }: Route.ComponentProps) {
  const { email } = loaderData;

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

  const htmlify = useCallback((text: string) => {
    return text
      .split(/\r?\n/)
      .map((line: string) => {
        const linkedLine = line.replace(
          /(https?:\/\/[^\s]+)/g,
          '<a href="$1" class="link-primary">$1</a>',
        );
        return `<p>${linkedLine}</p>`;
      })
      .join('');
  }, []);

  return (
    <dialog className="modal" open={true}>
      <div className="modal-box">
        <h3 className="mb-2 font-bold text-lg"> Email Preview </h3>
        <div className="mb-2 flex flex-col gap-2 text-sm">
          <div>
            <span className="fieldset-label">To</span>
            {email.to}
          </div>
          <div>
            <span className="fieldset-label">Subject</span>
            {email.subject}
          </div>
          <div>
            <span className="fieldset-label">State</span>
            <span
              className={`status status-${getStatusColor(email.state)} mx-3`}
            />
            <span
              className={email.state === 'bounced' ? 'tooltip' : ''}
              data-tip={email.bounceDescription || ''}
            >
              {capitalize(email.state)} at{' '}
              {prettyDate(email.stateUpdatedAt || email.createdAt)}
            </span>
          </div>
        </div>
        <div
          className="flex flex-col gap-2 overflow-x-scroll rounded bg-white p-5 text-black"
          // biome-ignore lint/security/noDangerouslySetInnerHtml: <explanation>
          dangerouslySetInnerHTML={{ __html: htmlify(email.body) }}
        />
        <div className="modal-action">
          <button type="submit" form="form-dismiss" className="btn">
            <FaUndo />
            Close
          </button>
        </div>
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
