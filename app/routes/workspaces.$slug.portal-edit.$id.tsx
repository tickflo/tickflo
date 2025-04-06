import { FaPlus, FaPuzzlePiece } from 'react-icons/fa';
import { data } from 'react-router';
import { appContext } from '~/app-context';

import { errorRedirect } from '~/.server/helpers';
import {
  getPortalQuestionsById,
  getPortalSectionsById,
} from '~/.server/services/portal';
import { getPortalById } from '~/.server/services/portal/get-portal-by-id';
import type { Route } from './+types/workspaces.$slug.portal-edit.$id';

export async function loader({ context, params }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session } = ctx;

  const portalId = Number.parseInt(params.id, 10);
  const portal = await getPortalById({ slug: params.slug, id: portalId }, ctx);

  const sections = await getPortalSectionsById(
    { slug: params.slug, id: portalId },
    ctx,
  );

  const questions = await getPortalQuestionsById(
    { slug: params.slug, id: portalId },
    ctx,
  );

  if (portal.isErr() || sections.isErr() || questions.isErr()) {
    const message =
      (portal.isErr() && portal.error.message) ||
      (sections.isErr() && sections.error.message) ||
      (questions.isErr() && questions.error.message) ||
      'Unknown error';

    return errorRedirect(session, message, '../portals');
  }

  return data({
    portal: portal.value,
    sections: sections.value,
    questions: questions.value,
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

        <div className="flex gap-2">
          <div className="card w-full bg-base-100 shadow-2xl">
            <div className="card-body">
              <h2 className="card-title">Designer</h2>
              <div className="mb-2 flex flex-row justify-end gap-2">
                <input type="text" className="input" placeholder="Title" />
                <button className="btn btn-sm btn-success h-9" type="button">
                  <FaPlus />
                  Add Section
                </button>
              </div>
              <hr className="mb-2" />
            </div>
          </div>
          <div className="card w-full bg-base-100 shadow-2xl">
            <div className="card-body">
              <h2 className="card-title">Questions</h2>
              <div className="mb-2 flex gap-2">
                <fieldset className="fieldset w-full">
                  <label className="fieldset-label" htmlFor="question-label">
                    Label
                  </label>
                  <input
                    id="question-label"
                    type="text"
                    className="input"
                    placeholder="Label"
                  />

                  <label className="fieldset-label" htmlFor="question-type">
                    Type
                  </label>
                  <select className="select" id="question-type">
                    <option value="1">Short Text</option>
                  </select>
                </fieldset>

                <fieldset className="fieldset w-full">
                  <label className="fieldset-label" htmlFor="question-value">
                    Default value
                  </label>
                  <input
                    id="question-value"
                    type="text"
                    className="input"
                    placeholder="Default value"
                  />
                  <label className="fieldset-label" htmlFor="question-field">
                    System Field
                  </label>
                  <select className="select" id="question-field">
                    <option value="1">Contact Name</option>
                  </select>
                </fieldset>
                <button
                  className="btn btn-sm btn-success mb-2 self-end"
                  type="button"
                >
                  <FaPlus />
                  Add Question
                </button>
              </div>
              <hr className="mb-2" />
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
