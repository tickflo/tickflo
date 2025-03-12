import { and, eq } from 'drizzle-orm';
import { db } from '.';
import { defaultPermissions } from '../permissions';
import { permissions } from './schema';

export async function insertMissingPermissions() {
  for (const resource in defaultPermissions) {
    // biome-ignore lint/suspicious/noPrototypeBuiltins: <explanation>
    if (defaultPermissions.hasOwnProperty(resource)) {
      // @ts-ignore
      const actions = defaultPermissions[resource];
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
