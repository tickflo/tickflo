interface Users {
  create: boolean;
  read: boolean;
  update: boolean;
  delete: boolean;
}

interface Roles {
  create: boolean;
  read: boolean;
  update: boolean;
  delete: boolean;
}

export interface Permissions {
  users: Users;
  roles: Roles;
}

export type Action = 'create' | 'read' | 'update' | 'delete';
export function isAction(arg: string): arg is Action {
  return (
    arg === 'create' || arg === 'read' || arg === 'update' || arg === 'delete'
  );
}

export function defaultAdminPermissions(): Permissions {
  return {
    users: { create: true, read: true, update: true, delete: true },
    roles: { create: true, read: true, update: true, delete: true },
  };
}

export function defaultUserPermissions(): Permissions {
  return {
    users: { create: false, read: false, update: false, delete: false },
    roles: { create: false, read: false, update: false, delete: false },
  };
}
