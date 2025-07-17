import { FaPlus, FaPuzzlePiece } from 'react-icons/fa';
import { data } from 'react-router';
import { appContext } from '~/app-context';

import React, { useState } from 'react';
import { errorRedirect } from '~/.server/helpers';
import {
  type Section,
  getPortalQuestionsById,
  getPortalSectionsById,
} from '~/.server/services/portal';
import { getPortalById } from '~/.server/services/portal/get-portal-by-id';
import { getQuestionField } from '~/question-fields';
import { QuestionType, getQuestionType } from '~/question-types';
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

type PortalSectionProps = {
  id: number;
  title: string | null;
  questions: PortalQuestionProps[];
};

function PortalSection({ id, title, questions }: PortalSectionProps) {
  return (
    <div className="card mb-2 w-full cursor-move border border-secondary bg-base-200 shadow-sm">
      <div className="card-body px-4 py-2">
        <h2 className="card-title">{title || 'Untitled'}</h2>
        <div className="flex flex-col">
          {questions.map((question) => (
            <PortalQuestion
              key={question.id}
              id={question.id}
              sectionId={id}
              label={question.label}
              typeId={question.typeId}
              fieldId={question.fieldId}
              defaultValue={question.defaultValue}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

type PortalQuestionProps = {
  id: number;
  sectionId: number | null;
  label: string;
  typeId: number;
  fieldId: number | null;
  defaultValue: string | null;
};

function PortalQuestion({
  id: __,
  label,
  typeId,
  fieldId,
  defaultValue,
}: PortalQuestionProps) {
  return (
    <div className="card-body py-1">
      <div className="flex flex-wrap items-center gap-4 text-sm">
        <span>
          <span className="font-semibold text-primary">Label:</span> {label}
        </span>
        <span>
          <span className="font-semibold text-primary">Type:</span>{' '}
          {getQuestionType(typeId)}
        </span>
        {fieldId && (
          <span>
            <span className="font-semibold text-primary">System field:</span>{' '}
            {getQuestionField(fieldId)}
          </span>
        )}
        {defaultValue && (
          <span>
            <span className="font-semibold text-primary">Default:</span>{' '}
            {defaultValue}
          </span>
        )}
      </div>
    </div>
  );
}

function sortSections(sections: Section[]) {
  sections.sort((a, b) => a.rank.localeCompare(b.rank));

  for (const section of sections) {
    section.questions.sort((a, b) => a.rank.localeCompare(b.rank));
  }

  return sections;
}

export default function workspacePortalEdit({
  loaderData,
}: Route.ComponentProps) {
  const { portal } = loaderData;

  const [sections, _setSections] = useState(sortSections(loaderData.sections));
  const [questions, _setQuestions] = useState(loaderData.questions);

  return (
    <>
      <div>
        <h1 className="mb-2 text-2xl">
          <FaPuzzlePiece className="inline pb-1 pl-1" /> Edit Portal -{' '}
          {portal.name}
        </h1>

        <hr className="mb-2" />

        <div className="mb-2 flex gap-2">
          <div className="card w-full flex-2/3 bg-base-100 shadow-2xl">
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
              <div className="flex flex-col gap-2">
                {sections.map((section) => {
                  const sectionQuestions = section.questions
                    .map((q) => {
                      const question = questions.find((qu) => qu.id === q.id);
                      if (!question) {
                        return null;
                      }

                      return {
                        id: question.id,
                        sectionId: section.id,
                        label: question.label,
                        typeId: question.typeId,
                        fieldId: question.fieldId,
                        defaultValue: question.defaultValue,
                      } satisfies PortalQuestionProps;
                    })
                    .filter((q) => !!q);

                  return (
                    <PortalSection
                      id={section.id}
                      title={section.title}
                      key={section.id}
                      questions={sectionQuestions}
                    />
                  );
                })}
              </div>
            </div>
          </div>

          <div className="card w-full flex-1/3 bg-base-100 shadow-2xl">
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
              <div className="flex flex-col gap-2">
                {questions.map((question) => (
                  <PortalQuestion
                    key={question.id}
                    id={question.id}
                    sectionId={null}
                    label={question.label}
                    typeId={question.typeId}
                    fieldId={question.fieldId}
                    defaultValue={question.defaultValue}
                  />
                ))}
              </div>
            </div>
          </div>
        </div>

        <div className="card w-full bg-base-100 shadow-2xl">
          <div className="card-body">
            <h2 className="card-title">Preview</h2>
            {sections.map((section) => (
              <fieldset key={section.id} className="fieldset">
                {section.title && (
                  <legend className="fieldset-legend">{section.title}</legend>
                )}
                {section.questions.map(({ id: questionId }) => {
                  const question = questions.find((q) => q.id === questionId);
                  if (!question) {
                    return null;
                  }

                  return (
                    <React.Fragment key={questionId}>
                      <label
                        className="label"
                        htmlFor={`question-${questionId}`}
                      >
                        {question.label}
                      </label>
                      {question.typeId === QuestionType.ShortText && (
                        <input
                          id={`question-${questionId}`}
                          type="text"
                          className="input"
                          placeholder={question.label}
                        />
                      )}
                      {question.typeId === QuestionType.LongText && (
                        <textarea
                          id={`question-${questionId}`}
                          className="textarea"
                          placeholder={question.label}
                        />
                      )}
                    </React.Fragment>
                  );
                })}
              </fieldset>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}
