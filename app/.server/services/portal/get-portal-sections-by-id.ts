import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import {
  portalQuestions,
  portalSectionQuestions,
  portalSections,
  portals,
  workspaces,
} from '~/.server/db/schema';
import { type ApiError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';

export type Section = {
  id: number;
  title: string | null;
  questions: Question[];
  conditionId: number | null;
  rank: string;
};

type Question = {
  id: number;
  conditionId: number | null;
  rank: string;
};

export async function getPortalSectionsById(
  { slug, id }: { slug: string; id: number },
  context: Context,
): Promise<Result<Section[], ApiError>> {
  const { tx } = context;
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.portals.read) {
    return Err(new PermissionsError('You do not have access to portals'));
  }

  const result = await (tx || db)
    .select()
    .from(portals)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, portals.workspaceId), eq(workspaces.slug, slug)),
    )
    .innerJoin(portalSections, eq(portalSections.portalId, portals.id))
    .innerJoin(
      portalSectionQuestions,
      eq(portalSectionQuestions.sectionId, portalSections.id),
    )
    .innerJoin(
      portalQuestions,
      eq(portalQuestions.id, portalSectionQuestions.questionId),
    )
    .where(eq(portals.id, id))
    .orderBy(portalSections.rank, portalSectionQuestions.rank);

  const sectionIds = result
    .map((r) => r.portal_sections.id)
    .filter((id, index, arr) => arr.indexOf(id) === index);

  return Ok(
    sectionIds.map((id) => {
      const rows = result.filter((r) => r.portal_sections.id === id);
      return {
        id,
        title: rows[0].portal_sections.title,
        conditionId: rows[0].portal_sections.conditionId,
        rank: rows[0].portal_sections.rank,
        questions: rows.map((r) => ({
          id: r.portal_section_questions.questionId,
          conditionId: r.portal_section_questions.conditionId,
          rank: rows[0].portal_section_questions.rank,
        })),
      };
    }),
  );
}
