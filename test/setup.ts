import { setupDb } from 'server/setup-db';
import { beforeAll, vi } from 'vitest';

// Replace the database with a new in-memory database
vi.mock('~/.server/db', async () => {
  const { PGlite } = await import('@electric-sql/pglite');
  const { drizzle } = await import('drizzle-orm/pglite');
  const { db: actual } =
    await vi.importActual<typeof import('~/.server/db')>('~/.server/db');
  const schema = await import('~/.server/db/schema');
  const client = new PGlite();
  const db = drizzle(client, { schema });
  return {
    ...actual,
    db,
  };
});

beforeAll(async () => {
  await setupDb();
});
