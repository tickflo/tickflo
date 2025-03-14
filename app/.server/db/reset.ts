import { sql } from 'drizzle-orm';
import { setupDb } from 'server/setup-db';
import { db } from '~/.server/db';

export async function reset() {
  await db.execute(sql`drop schema if exists public cascade`);
  await db.execute(sql`create schema public`);
  await db.execute(sql`drop schema if exists drizzle cascade`);

  await setupDb();
}
