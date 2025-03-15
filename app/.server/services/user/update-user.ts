import { and, eq, inArray } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { userWorkspaceRoles, users } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { getPermissions } from '../security';
import { getRoleIdsForUserId } from '../security';
import { getWorkspaceBySlug } from '../workspace';

export async function updateUser(
  {
    slug,
    userId,
    roleIds,
  }: { slug: string; userId: number; roleIds: number[] },
  context: Context,
): Promise<Result<void, ApiError>> {
  const user = context.user.unwrap();
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.users.update) {
    return Err(
      new PermissionsError('You do not have permission to update users'),
    );
  }

  if (!roleIds.length) {
    return Err(new InputError('You must select at least one role'));
  }

  const existingRoleIds = await getRoleIdsForUserId(
    { id: userId, slug },
    context,
  );

  if (existingRoleIds.isErr()) {
    return Err(existingRoleIds.error);
  }

  const deleteIds = existingRoleIds.value.filter(
    (id) => roleIds.indexOf(id) === -1,
  );
  const insertIds = roleIds.filter(
    (id) => existingRoleIds.value.indexOf(id) === -1,
  );

  await db.transaction(async (tx) => {
    if (deleteIds.length) {
      await tx
        .delete(userWorkspaceRoles)
        .where(
          and(
            inArray(userWorkspaceRoles.roleId, deleteIds),
            eq(userWorkspaceRoles.userId, userId),
          ),
        );
    }

    if (insertIds.length) {
      const workspace = (await getWorkspaceBySlug({ slug }, context)).unwrap();
      await tx.insert(userWorkspaceRoles).values(
        insertIds.map((id) => ({
          workspaceId: workspace.id,
          userId,
          roleId: id,
          createdBy: user.id,
        })),
      );
    }

    if (deleteIds.length || insertIds.length) {
      await tx.update(users).set({
        updatedBy: user.id,
      });
    }
  });

  return Ok.EMPTY;
}
