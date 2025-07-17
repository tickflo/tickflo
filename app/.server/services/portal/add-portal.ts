import { LexoRank } from 'lexorank';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  portalQuestions,
  portalSectionQuestions,
  portalSections,
  portals,
} from '~/.server/db/schema';
import { ApiError, InputError, PermissionsError } from '~/.server/errors';
import { QuestionField } from '~/question-fields';
import { QuestionType } from '~/question-types';
import { slugify } from '~/utils/slugify';
import { getPermissions } from '../security';
import { getWorkspaceBySlug } from '../workspace';

type Request = {
  slug: string;
  name: string | undefined;
};

type Response = {
  id: number;
};

export async function addPortal(
  { slug, name }: Request,
  context: Context,
): Promise<Result<Response, ApiError>> {
  const user = context.user.unwrap();
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.portals.create) {
    return Err(
      new PermissionsError('You do not have permission to add portals'),
    );
  }

  const { config } = context;

  if (
    !name ||
    name.length < config.PORTAL.MIN_NAME_LENGTH ||
    name.length > config.PORTAL.MAX_NAME_LENGTH
  ) {
    return Err(
      new InputError(
        `Name must be between ${config.PORTAL.MIN_NAME_LENGTH} and ${config.PORTAL.MAX_NAME_LENGTH} characters in length`,
      ),
    );
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);
  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  return db.transaction(async (tx) => {
    const inserted = await tx
      .insert(portals)
      .values({
        workspaceId: workspace.value.id,
        name,
        slug: slugify(name),
        createdBy: user.id,
      })
      .returning({
        id: portals.id,
      });

    if (!inserted.length) {
      return Err(new ApiError('Failed to create portal'));
    }

    const portalId = inserted[0].id;

    const questions = await tx
      .insert(portalQuestions)
      .values([
        {
          label: 'Your name',
          portalId,
          typeId: QuestionType.ShortText,
          fieldId: QuestionField.ContactName,
          createdBy: user.id,
        },
        {
          label: 'Your Email',
          portalId,
          typeId: QuestionType.ShortText,
          fieldId: QuestionField.ContactEmail,
          createdBy: user.id,
        },
        {
          label: 'Describe your issue',
          portalId,
          typeId: QuestionType.LongText,
          fieldId: QuestionField.TicketDescription,
          createdBy: user.id,
        },
      ])
      .returning({ id: portalQuestions.id });

    if (!questions.length) {
      return Err(new ApiError('Failed to create portal'));
    }

    const rank = LexoRank.middle();

    const sections = await tx
      .insert(portalSections)
      .values({
        portalId,
        rank: rank.toString(),
        createdBy: user.id,
      })
      .returning({ id: portalSections.id });

    if (!sections.length) {
      return Err(new ApiError('Failed to create portal'));
    }

    const sectionId = sections[0].id;

    await tx.insert(portalSectionQuestions).values(
      questions.map((q, index) => {
        let rank = LexoRank.middle();

        for (let i = 0; i < index; ++i) {
          rank = rank.genNext();
        }

        return {
          sectionId,
          questionId: q.id,
          rank: rank.toString(),
          createdBy: user.id,
        };
      }),
    );

    return Ok({ id: portalId });
  });
}
