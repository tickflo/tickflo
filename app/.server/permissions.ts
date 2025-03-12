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

export const defaultAdminPermissions: Permissions = {
  users: { create: true, read: true, update: true, delete: true },
  roles: { create: true, read: true, update: true, delete: true },
};

export const defaultUserPermissions: Permissions = {
  users: { create: false, read: false, update: false, delete: false },
  roles: { create: false, read: false, update: false, delete: false },
};
