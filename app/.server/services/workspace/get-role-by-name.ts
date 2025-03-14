import { and, eq } from 'drizzle-orm';
import { None, type Option, Some } from 'ts-results-es';
import type { Context } from '~/.server/context';
import { db } from '~/.server/db';
import { roles, workspaces } from '~/.server/db/schema';

type Role = typeof roles.$inferSelect;

export async function getRoleByName(
  { slug, name }: { slug: string; name: string },
  context: Context,
): Promise<Option<Role>> {
  const { tx } = context;

  const result = await (tx || db)
    .select()
    .from(roles)
    .innerJoin(
      workspaces,
      and(eq(workspaces.id, roles.workspaceId), eq(workspaces.slug, slug)),
    )
    .where(eq(roles.role, name));

  if (!result.length) {
    return None;
  }

  return Some(result[0].roles);
}
