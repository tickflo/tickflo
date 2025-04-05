import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { type ApiError, PermissionsError } from '~/.server/errors';
import { portals, workspaces } from '../../db/schema';
import { getPermissions } from '../security';

type Portal = typeof portals.$inferSelect;

export async function getPortals(
  { slug }: { slug: string },
  context: Context,
): Promise<Result<Portal[], ApiError>> {
  const { tx } = context;

  const permissions = await getPermissions({ slug }, context);
  if (!permissions.portals.read) {
    return Err(
      new PermissionsError('You do not have permission to view portals'),
    );
  }

  const rows = await (tx || db)
    .select()
    .from(portals)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, portals.workspaceId), eq(workspaces.slug, slug)),
    );

  return Ok(rows.map((r) => r.portals));
}
