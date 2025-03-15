import { randomBytes } from 'node:crypto';
import { and, eq } from 'drizzle-orm';
import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '../../db';
import { userWorkspaceRoles, userWorkspaces, users } from '../../db/schema';
import { ApiError, InputError, PermissionsError } from '../../errors';
import { getPermissions } from '../security';
import { getUserByEmail } from '../user';
import { getWorkspaceBySlug } from '../workspace';
import { sendInviteEmail } from './send-invite-email';

export async function addUser(
  {
    slug,
    roleIds,
    ...request
  }: {
    slug: string;
    name: string | undefined;
    email: string | undefined;
    roleIds: number[];
  },
  context: Context,
): Promise<Result<void, InputError>> {
  const name = request.name?.toString().trim();
  const email = request.email?.toString().trim().toLowerCase();

  const { config } = context;

  const permissions = await getPermissions({ slug }, context);
  if (!permissions.users.create) {
    return Err(new PermissionsError('You do not have permission to add users'));
  }

  const user = context.user.unwrap();

  if (
    !name ||
    name.trim().length < config.USER.MIN_NAME_LENGTH ||
    name.trim().length > config.USER.MAX_NAME_LENGTH
  ) {
    return Err(
      new InputError(
        `Name must be between ${config.USER.MIN_NAME_LENGTH} and ${config.USER.MAX_NAME_LENGTH} characters in length`,
      ),
    );
  }

  if (!email) {
    return Err(new InputError('Email is required'));
  }

  if (!roleIds.length) {
    return Err(new InputError('You must select at least one role'));
  }

  for (const roleId of roleIds) {
    if (Number.isNaN(roleId)) {
      return Err(new InputError('Invalid Role'));
    }
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);

  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  const existingWorkspace = await db
    .select({ id: users.id })
    .from(userWorkspaces)
    .innerJoin(
      users,
      and(eq(users.email, email), eq(users.id, userWorkspaces.userId)),
    )
    .where(eq(userWorkspaces.workspaceId, workspace.value.id));

  if (existingWorkspace && existingWorkspace.length > 0) {
    return Err(
      new InputError(`${email} is already a member of this workspace`),
    );
  }

  const existing = await getUserByEmail({ email }, context);
  if (existing.isSome()) {
    return await db.transaction(async (tx) => {
      await tx.insert(userWorkspaces).values({
        userId: existing.value.id,
        workspaceId: workspace.value.id,
        createdBy: user.id,
      });

      await tx.insert(userWorkspaceRoles).values(
        roleIds.map((id) => ({
          roleId: id,
          userId: existing.value.id,
          workspaceId: workspace.value.id,
          createdBy: user.id,
        })),
      );

      const result = await sendInviteEmail(
        { userId: existing.value.id, slug },
        { ...context, tx },
      );
      if (result.isErr()) {
        return Err(result.error);
      }

      return Ok.EMPTY;
    });
  }

  const emailConfirmationCode = randomBytes(32).toString('hex');

  return await db.transaction(async (tx) => {
    const userRows = await tx
      .insert(users)
      .values({
        name,
        email,
        emailConfirmationCode,
        createdBy: user.id,
      })
      .returning({ id: users.id });

    if (!userRows || userRows.length === 0) {
      return Err(new ApiError('Failed to insert user record'));
    }

    const newUser = userRows[0];

    await tx.insert(userWorkspaces).values({
      userId: newUser.id,
      workspaceId: workspace.value.id,
      createdBy: user.id,
    });

    try {
      await tx.insert(userWorkspaceRoles).values(
        roleIds.map((id) => ({
          userId: newUser.id,
          workspaceId: workspace.value.id,
          roleId: id,
          createdBy: user.id,
        })),
      );
    } catch (e) {
      if (e instanceof Error) {
        if (e.message.indexOf('foreign key') > -1) {
          return Err(new InputError('Invalid role id'));
        }

        throw e;
      }
    }

    const result = await sendInviteEmail(
      { userId: newUser.id, slug },
      { ...context, tx },
    );

    if (result.isErr()) {
      return Err(result.error);
    }

    return Ok.EMPTY;
  });
}
