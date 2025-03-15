import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import {
  userWorkspaceRoles,
  userWorkspaces,
  users,
  workspaces,
} from '../../db/schema';
import { getPermissions } from '../security';

export async function getRoleIdsForUserId(
  { id, slug }: { id: number; slug: string },
  context: Context,
): Promise<Result<number[], ApiError>> {
  const { tx } = context;
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.users.read) {
    return Err(new PermissionsError('You do not have access users'));
  }

  const rows = await (tx || db)
    .select({
      id: userWorkspaceRoles.roleId,
    })
    .from(users)
    .innerJoin(userWorkspaces, eq(userWorkspaces.userId, users.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaces.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .innerJoin(
      userWorkspaceRoles,
      and(
        eq(userWorkspaceRoles.userId, users.id),
        eq(userWorkspaceRoles.workspaceId, workspaces.id),
      ),
    )
    .where(eq(users.id, id));

  if (!rows.length) {
    return Err(new InputError('User not found'));
  }

  return Ok(rows.map((r) => r.id));
}
