export type Resource = 'users' | 'roles' | 'portals';
export type Action = 'create' | 'read' | 'update' | 'delete';

export type LoaderPermissions = {
  users: Action[];
  roles: Action[];
  portals: Action[];
};

export const defaultLoaderPermissions: LoaderPermissions = {
  users: [],
  roles: [],
  portals: [],
};

export function isAction(arg: string): arg is Action {
  return (
    arg === 'create' || arg === 'read' || arg === 'update' || arg === 'delete'
  );
}

export const RESOURCES: {
  key: Resource;
  label: string;
}[] = [
  { key: 'users', label: 'Users' },
  { key: 'roles', label: 'Roles' },
  { key: 'portals', label: 'Portals' },
];

export const ACTIONS: {
  action: Action;
  label: string;
}[] = [
  { action: 'create', label: 'Add' },
  { action: 'read', label: 'View' },
  { action: 'update', label: 'Update' },
  { action: 'delete', label: 'Remove' },
];
