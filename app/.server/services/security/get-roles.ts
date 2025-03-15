import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { type ApiError, PermissionsError } from '~/.server/errors';
import { roles, type roles as rolesType, workspaces } from '../../db/schema';
import { getPermissions } from './get-permissions';

type Role = typeof rolesType.$inferSelect;

export async function getRoles(
  { slug }: { slug: string },
  context: Context,
): Promise<Result<Role[], ApiError>> {
  const { tx } = context;

  const permissions = await getPermissions({ slug }, context);
  if (!permissions.roles.read) {
    return Err(
      new PermissionsError('You do not have permission to view roles'),
    );
  }

  const rows = await (tx || db)
    .select()
    .from(roles)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, roles.workspaceId), eq(workspaces.slug, slug)),
    );

  return Ok(rows.map((r) => r.roles));
}
