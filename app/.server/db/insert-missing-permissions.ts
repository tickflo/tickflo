import { and, eq } from 'drizzle-orm';
import { db } from '.';
import { defaultUserPermissions } from '../permissions';
import { permissions } from './schema';

export async function insertMissingPermissions() {
  for (const resource in defaultUserPermissions) {
    // biome-ignore lint/suspicious/noPrototypeBuiltins: <explanation>
    if (defaultUserPermissions.hasOwnProperty(resource)) {
      // @ts-ignore
      const actions = defaultUserPermissions[resource];
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
