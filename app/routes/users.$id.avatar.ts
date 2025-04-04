import { redirect } from 'react-router';
import { getFile } from '~/.server/storage';
import defaultAvatar from '~/avatar.png';
import type { Route } from './+types/users.$id.avatar';

export async function loader({ params }: Route.LoaderArgs) {
  const userId = Number.parseInt(params.id, 10);
  if (Number.isNaN(userId)) {
    throw new Error(`Invalid user id: ${params.id}`);
  }

  const buffer = await getFile(`user-data/${userId}/avatar.jpg`);
  if (buffer.isNone()) {
    return redirect(defaultAvatar, 302);
  }

  return new Response(buffer.value.buffer, {
    headers: {
      'Content-Type': buffer.value.contentType || 'image/jpeg',
    },
  });
}
