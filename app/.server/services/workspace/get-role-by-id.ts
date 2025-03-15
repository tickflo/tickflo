import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles, workspaces } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';

type Role = typeof roles.$inferSelect;

export async function getRoleById(
  { slug, id }: { slug: string; id: number },
  context: Context,
): Promise<Result<Role, ApiError>> {
  const { tx } = context;
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.roles.read) {
    return Err(new PermissionsError('You do not have access to roles'));
  }

  const result = await (tx || db)
    .select()
    .from(roles)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, roles.workspaceId), eq(workspaces.slug, slug)),
    )
    .where(eq(roles.id, id));

  if (!result.length) {
    return Err(new InputError('Role not found'));
  }

  return Ok(result[0].roles);
}
