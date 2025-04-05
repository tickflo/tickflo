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

interface Portals {
  create: boolean;
  read: boolean;
  update: boolean;
  delete: boolean;
}

export interface Permissions {
  users: Users;
  roles: Roles;
  portals: Portals;
}

export function defaultAdminPermissions(): Permissions {
  return {
    users: { create: true, read: true, update: true, delete: true },
    roles: { create: true, read: true, update: true, delete: true },
    portals: { create: true, read: true, update: true, delete: true },
  };
}

export function defaultUserPermissions(): Permissions {
  return {
    users: { create: false, read: false, update: false, delete: false },
    roles: { create: false, read: false, update: false, delete: false },
    portals: { create: false, read: false, update: false, delete: false },
  };
}
