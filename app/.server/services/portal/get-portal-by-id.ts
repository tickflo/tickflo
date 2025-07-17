import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { portals, workspaces } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';

type Portal = typeof portals.$inferSelect;

export async function getPortalById(
  { slug, id }: { slug: string; id: number },
  context: Context,
): Promise<Result<Portal, ApiError>> {
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
    .where(eq(portals.id, id));

  if (!result.length) {
    return Err(new InputError('Portal not found'));
  }

  return Ok(result[0].portals);
}
