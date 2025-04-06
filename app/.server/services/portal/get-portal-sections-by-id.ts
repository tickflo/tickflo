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

type Section = {
  id: number;
  title: string | null;
  questions: Question[];
};

type Question = {
  id: number;
  label: string;
  typeId: number;
  defaultValue: string | null;
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
    .where(eq(portals.id, id));

  const sectionIds = result
    .map((r) => r.portal_sections.id)
    .filter((id, index, arr) => arr.indexOf(id) === index);

  return Ok(
    sectionIds.map((id) => {
      const rows = result.filter((r) => r.portal_sections.id === id);
      return {
        id,
        title: rows[0].portal_sections.title,
        questions: rows.map((r) => ({
          id: r.portal_questions.id,
          label: r.portal_questions.label,
          typeId: r.portal_questions.typeId,
          defaultValue: r.portal_questions.defaultValue,
        })),
      };
    }),
  );
}
