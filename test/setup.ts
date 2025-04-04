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

/*
vi.mock('~/.server/storage', async () => {
  const { mockClient } = await import('aws-sdk-client-mock');
  const storage =
    await vi.importActual<typeof import('~/.server/storage')>(
      '~/.server/storage',
    );
  return {
    ...storage,
    s3: mockClient(storage.s3),
  };
});
*/

beforeAll(async () => {
  await setupDb();
});
