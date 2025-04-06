import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { portalQuestions, portals, workspaces } from '~/.server/db/schema';
import { type ApiError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';

export async function getPortalQuestionsById(
  { slug, id }: { slug: string; id: number },
  context: Context,
): Promise<Result<number[], ApiError>> {
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
    .innerJoin(portalQuestions, eq(portalQuestions.portalId, portals.id))
    .where(eq(portals.id, id));

  return Ok(result.map((r) => r.portal_questions.id));
}
