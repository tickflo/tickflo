import { Err, Ok, type Result } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { portals } from '~/.server/db/schema';
import { type ApiError, InputError, PermissionsError } from '~/.server/errors';
import { slugify } from '~/utils/slugify';
import { getPermissions } from '../security';
import { getWorkspaceBySlug } from '../workspace';

type Request = {
  slug: string;
  name: string | undefined;
};

export async function addPortal(
  { slug, name }: Request,
  context: Context,
): Promise<Result<void, ApiError>> {
  const user = context.user.unwrap();
  const permissions = await getPermissions({ slug }, context);
  if (!permissions.portals.create) {
    return Err(
      new PermissionsError('You do not have permission to add portals'),
    );
  }

  const { config } = context;

  if (
    !name ||
    name.length < config.PORTAL.MIN_NAME_LENGTH ||
    name.length > config.PORTAL.MAX_NAME_LENGTH
  ) {
    return Err(
      new InputError(
        `Name must be between ${config.PORTAL.MIN_NAME_LENGTH} and ${config.PORTAL.MAX_NAME_LENGTH} characters in length`,
      ),
    );
  }

  const workspace = await getWorkspaceBySlug({ slug }, context);
  if (workspace.isNone()) {
    return Err(new InputError(`Workspace ${slug} does not exist`));
  }

  await db.insert(portals).values({
    workspaceId: workspace.value.id,
    name,
    slug: slugify(name),
    createdBy: user.id,
  });

  return Ok.EMPTY;
}
