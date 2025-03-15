import { and, eq } from 'drizzle-orm';
import { db } from '.';
import { defaultUserPermissions } from '../permissions';
import { permissions } from './schema';

export async function insertMissingPermissions() {
  const perms = defaultUserPermissions();
  for (const resource in perms) {
    // biome-ignore lint/suspicious/noPrototypeBuiltins: <explanation>
    if (perms.hasOwnProperty(resource)) {
      // @ts-ignore
      const actions = perms[resource];
      for (const action in actions) {
        const row = await db.query.permissions.findFirst({
          where: and(
            eq(permissions.resource, resource),
            eq(permissions.action, action),
          ),
        });

        if (!row) {
          await db.insert(permissions).values({
            resource,
            action,
          });
        }
      }
    }
  }
}
