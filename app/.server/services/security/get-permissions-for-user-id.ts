import type { Option } from 'ts-results-es';
import type { Context } from '~/.server/context';
import type { Permissions } from '~/.server/permissions';

export async function getPermissionsForUserId(
  { id }: { id: number },
  _context: Context,
): Option<Permissions> {}
