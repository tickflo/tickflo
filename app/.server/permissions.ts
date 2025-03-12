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

export const defaultPermissions: Permissions = {
  users: { create: false, read: false, update: false, delete: false },
  roles: { create: false, read: false, update: false, delete: false },
};
