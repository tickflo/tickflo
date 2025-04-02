import { max, sql } from 'drizzle-orm';
import { db } from '.';
import { systemEmailTemplates, workspaceEmailTemplates } from '../data';
import { emailTemplates, workspaces } from './schema';

export async function insertMissingEmailTemplates() {
  const workspaceRows = await db
    .select({ id: workspaces.id, createdBy: workspaces.createdBy })
    .from(workspaces);

  const workspaceTemplates = workspaceRows.flatMap((workspace) =>
    workspaceEmailTemplates.map((t) => ({
      workspaceId: workspace.id,
      templateTypeId: t.typeId,
      subject: t.subject,
      body: t.body,
      createdBy: workspace.createdBy,
    })),
  );

  const systemTemplates = systemEmailTemplates.map((t) => ({
    id: t.typeId,
    templateTypeId: t.typeId,
    subject: t.subject,
    body: t.body,
  }));

  await db.insert(emailTemplates).values(systemTemplates).onConflictDoNothing();

  const result = await db
    .select({ id: max(emailTemplates.id) })
    .from(emailTemplates);
  const maxId = result[0].id;
  if (!maxId || Number.isNaN(maxId)) {
    throw new Error('Invalid max id');
  }

  let nextId = maxId + 1;
  if (nextId < 100) {
    nextId = 101;
  }

  await db.execute(
    sql`ALTER SEQUENCE email_templates_id_seq RESTART WITH ${sql.raw(nextId.toString())}`,
  );

  if (!workspaceTemplates.length) {
    return;
  }

  await db
    .insert(emailTemplates)
    .values(workspaceTemplates)
    .onConflictDoNothing();
}
