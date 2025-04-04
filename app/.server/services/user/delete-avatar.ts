import type { Context } from '~/.server/context';
import { deleteFile } from '~/.server/storage';

export async function deleteAvatar(context: Context) {
  const user = context.user.unwrap();
  await deleteFile(`user-data/${user.id}/avatar.jpg`);
}
