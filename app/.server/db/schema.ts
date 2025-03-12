import { relations } from 'drizzle-orm';
import {
  boolean,
  integer,
  json,
  pgTable,
  primaryKey,
  text,
  timestamp,
  unique,
  varchar,
} from 'drizzle-orm/pg-core';
import config from '~/.server/config';

export const users = pgTable('users', {
  id: integer().primaryKey().generatedAlwaysAsIdentity(),
  name: varchar({ length: config.USER.MAX_NAME_LENGTH }).notNull(),
  email: varchar({ length: 254 }).notNull().unique(),
  emailConfirmed: boolean('email_confirmed').notNull().default(false),
  emailConfirmationCode: varchar('email_confirmation_code', { length: 100 }),
  passwordHash: varchar('password_hash', { length: 100 }),
  createdAt: timestamp('created_at', { withTimezone: true })
    .notNull()
    .defaultNow(),
  createdBy: integer('created_by'),
  updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
    () => new Date(),
  ),
  updatedBy: integer('updated_by'),
});

export const usersRelations = relations(users, ({ one, many }) => ({
  created_by_user: one(users, {
    fields: [users.createdBy],
    references: [users.id],
  }),
  updated_by_user: one(users, {
    fields: [users.updatedBy],
    references: [users.id],
  }),
  workspaces: many(workspaces),
  roles: many(userWorkspaceRoles),
}));

export const tokens = pgTable('tokens', {
  userId: integer('user_id')
    .notNull()
    .references(() => users.id, { onDelete: 'cascade' }),
  token: varchar({ length: 64 }).notNull(),
  createdAt: timestamp('created_at', { withTimezone: true })
    .notNull()
    .defaultNow(),
  maxAge: integer('max_age').notNull(),
});

export const tokensRelations = relations(tokens, ({ one }) => ({
  user: one(users, { fields: [tokens.userId], references: [users.id] }),
}));

export const emailTemplates = pgTable(
  'email_templates',
  {
    id: integer().primaryKey().generatedAlwaysAsIdentity(),
    workspaceId: integer('workspace_id').references(() => workspaces.id),
    templateTypeId: integer('template_type_id').notNull(),
    subject: text().notNull(),
    body: text().notNull(),
    createdAt: timestamp('created_at', { withTimezone: true })
      .notNull()
      .defaultNow(),
    createdBy: integer('created_by').references(() => users.id),
    updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
      () => new Date(),
    ),
    updatedBy: integer('updated_by').references(() => users.id),
  },
  (table) => [unique().on(table.workspaceId, table.templateTypeId)],
);

export const emailTemplatesRelations = relations(emailTemplates, ({ one }) => ({
  workspace: one(workspaces, {
    fields: [emailTemplates.workspaceId],
    references: [workspaces.id],
  }),
  createdByUser: one(users, {
    fields: [emailTemplates.createdBy],
    references: [users.id],
  }),
  updatedByUser: one(users, {
    fields: [emailTemplates.updatedBy],
    references: [users.id],
  }),
}));

export const emails = pgTable('emails', {
  id: integer().primaryKey().generatedAlwaysAsIdentity(),
  templateId: integer('template_id')
    .notNull()
    .references(() => emailTemplates.id),
  vars: json(),
  from: varchar({ length: 254 }).notNull().default('noreply@tickflo.co'),
  to: varchar({ length: 254 }).notNull(),
  createdAt: timestamp('created_at', { withTimezone: true })
    .notNull()
    .defaultNow(),
  createdBy: integer('created_by').references(() => users.id),
  updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
    () => new Date(),
  ),
  updatedBy: integer('updated_by').references(() => users.id),
});

export const emailsRelations = relations(emails, ({ one }) => ({
  template: one(emailTemplates, {
    fields: [emails.templateId],
    references: [emailTemplates.id],
  }),
  created_by_user: one(users, {
    fields: [emails.createdBy],
    references: [users.id],
  }),
  updated_by_user: one(users, {
    fields: [emails.updatedBy],
    references: [users.id],
  }),
}));

export const meta = pgTable('meta', {
  key: text().primaryKey(),
  value: text(),
});

export const workspaces = pgTable('workspaces', {
  id: integer().primaryKey().generatedAlwaysAsIdentity(),
  name: varchar({ length: config.WORKSPACE.MAX_NAME_LENGTH })
    .notNull()
    .unique(),
  slug: varchar({ length: config.WORKSPACE.MAX_SLUG_LENGTH })
    .notNull()
    .unique(),
  createdAt: timestamp('created_at', { withTimezone: true })
    .notNull()
    .defaultNow(),
  createdBy: integer('created_by')
    .notNull()
    .references(() => users.id),
  updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
    () => new Date(),
  ),
  updatedBy: integer('updated_by').references(() => users.id),
});

export const workspacesRelations = relations(workspaces, ({ one, many }) => ({
  createdByUser: one(users, {
    fields: [workspaces.createdBy],
    references: [users.id],
  }),
  roles: many(userWorkspaceRoles),
  emailTemplates: many(emailTemplates),
}));

export const roles = pgTable('roles', {
  id: integer().primaryKey().generatedAlwaysAsIdentity(),
  workspaceId: integer('workspace_id')
    .notNull()
    .references(() => workspaces.id),
  role: varchar({ length: config.ROLE.MAX_NAME_LENGTH }).notNull(),
  createdAt: timestamp('created_at', { withTimezone: true })
    .notNull()
    .defaultNow(),
  createdBy: integer('created_by')
    .notNull()
    .references(() => users.id),
  updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
    () => new Date(),
  ),
  updatedBy: integer('updated_by').references(() => users.id),
});

export const rolesPermissions = relations(roles, ({ many }) => ({
  permissions: many(rolePermissions),
}));

export const userWorkspaceRoles = pgTable(
  'user_workspace_roles',
  {
    userId: integer('user_id')
      .notNull()
      .references(() => users.id),
    workspaceId: integer('workspace_id')
      .notNull()
      .references(() => workspaces.id),
    roleId: integer('role_id')
      .notNull()
      .references(() => roles.id),
    accepted: boolean().notNull().default(false),
    createdAt: timestamp('created_at', { withTimezone: true })
      .notNull()
      .defaultNow(),
    createdBy: integer('created_by')
      .notNull()
      .references(() => users.id),
  },
  (table) => [primaryKey({ columns: [table.userId, table.workspaceId] })],
);

export const userWorkspaceRolesRelations = relations(
  userWorkspaceRoles,
  ({ one }) => ({
    role: one(roles, {
      fields: [userWorkspaceRoles.roleId],
      references: [roles.id],
    }),
    user: one(users, {
      fields: [userWorkspaceRoles.userId],
      references: [users.id],
    }),
    workspace: one(workspaces, {
      fields: [userWorkspaceRoles.workspaceId],
      references: [workspaces.id],
    }),
  }),
);

export const permissions = pgTable(
  'permissions',
  {
    id: integer().primaryKey().generatedAlwaysAsIdentity(),
    resource: text().notNull(),
    action: text().notNull(),
  },
  (table) => [unique().on(table.action, table.resource)],
);

export const rolePermissions = pgTable(
  'role_permissions',
  {
    roleId: integer('role_id')
      .notNull()
      .references(() => roles.id, { onDelete: 'cascade' }),
    permissionId: integer('permission_id')
      .notNull()
      .references(() => permissions.id, { onDelete: 'cascade' }),
    createdAt: timestamp('created_at', { withTimezone: true })
      .notNull()
      .defaultNow(),
    createdBy: integer('created_by').references(() => users.id),
    updatedAt: timestamp('updated_at', { withTimezone: true }).$onUpdateFn(
      () => new Date(),
    ),
    updatedBy: integer('updated_by').references(() => users.id),
  },
  (table) => [primaryKey({ columns: [table.roleId, table.permissionId] })],
);

export const rolePermissionsRelations = relations(
  rolePermissions,
  ({ many }) => ({
    permissions: many(permissions),
  }),
);
