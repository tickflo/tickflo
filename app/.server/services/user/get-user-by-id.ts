import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { userWorkspaces, users, workspaces } from '../../db/schema';
import { getPermissions } from '../security';

type User = typeof users.$inferSelect;

export async function getUserById(
  { id, slug }: { id: number; slug: string },
  context: Context,
): Promise<Result<User, ApiError>> {
  const { tx } = context;
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.users.read) {
    return Err(new PermissionsError('You do not have access users'));
  }

  const row = await (tx || db)
    .select()
    .from(users)
    .innerJoin(userWorkspaces, eq(userWorkspaces.userId, users.id))
    .innerJoin(
      workspaces,
      and(
        eq(workspaces.id, userWorkspaces.workspaceId),
        eq(workspaces.slug, slug),
      ),
    )
    .where(eq(users.id, id));

  if (!row.length) {
    return Err(new InputError('User not found'));
  }

  return Ok(row[0].users);
}
