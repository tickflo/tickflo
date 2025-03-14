import { Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { workspaceEmailTemplates } from '~/.server/data';
import { db } from '~/.server/db';
import {
  emailTemplates,
  roles,
  userWorkspaceRoles,
  userWorkspaces,
  workspaces,
} from '~/.server/db/schema';
import {
  defaultAdminPermissions,
  defaultUserPermissions,
} from '~/.server/permissions';
import { slugify } from '~/utils/slugify';
import { type ApiError, InputError } from '../../errors';
import { updatePermissionsForRole } from '../security';

type Request = {
  name: string | undefined;
};

type Response = {
  slug: string;
};

export async function createWorkspace(
  { name }: Request,
  context: Context,
): Promise<Result<Response, ApiError>> {
  const { tx, config } = context;

  const user = context.user.unwrap();

  if (
    !name ||
    name.length < config.WORKSPACE.MIN_NAME_LENGTH ||
    name.length > config.WORKSPACE.MAX_NAME_LENGTH
  ) {
    throw new InputError(
      `Workspace name must be between ${config.WORKSPACE.MIN_NAME_LENGTH} and ${config.WORKSPACE.MAX_NAME_LENGTH} characters in length`,
    );
  }

  const slug = slugify(name, config.WORKSPACE.MAX_SLUG_LENGTH);
  const workspaceRows = await (tx || db)
    .insert(workspaces)
    .values({
      name,
      slug,
      createdBy: user.id,
    })
    .returning({
      id: workspaces.id,
    });

  if (!workspaceRows || workspaceRows.length === 0) {
    throw new Error('Failed to insert workspace record');
  }

  const workspace = workspaceRows[0];

  const roleRows = await (tx || db)
    .insert(roles)
    .values([
      {
        workspaceId: workspace.id,
        name: 'Administrator',
        admin: true,
        createdBy: user.id,
      },
      {
        workspaceId: workspace.id,
        name: 'Technician',
        createdBy: user.id,
      },
    ])
    .returning({
      id: roles.id,
      name: roles.name,
    });

  if (!roleRows || roleRows.length !== 2) {
    throw new Error('Failed to insert role records');
  }

  const adminRole = roleRows.find((r) => r.name === 'Administrator');
  if (!adminRole) {
    throw new Error('Could not find Administrator role');
  }

  const technicianRole = roleRows.find((r) => r.name === 'Technician');
  if (!technicianRole) {
    throw new Error('Could not find Technicican role');
  }

  await updatePermissionsForRole(
    { roleId: adminRole.id, permissions: defaultAdminPermissions(), slug },
    context,
  );

  await updatePermissionsForRole(
    { roleId: technicianRole.id, permissions: defaultUserPermissions(), slug },
    context,
  );

  await (tx || db).insert(userWorkspaces).values({
    accepted: true,
    userId: user.id,
    workspaceId: workspace.id,
    createdBy: user.id,
  });

  await (tx || db).insert(userWorkspaceRoles).values({
    userId: user.id,
    workspaceId: workspace.id,
    roleId: adminRole.id,
    createdBy: user.id,
  });

  await (tx || db).insert(emailTemplates).values(
    workspaceEmailTemplates.map((t) => ({
      workspaceId: workspace.id,
      templateTypeId: t.typeId,
      subject: t.subject,
      body: t.body,
      createdBy: user.id,
    })),
  );

  return Ok({ slug });
}
