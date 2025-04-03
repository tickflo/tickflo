import sharp from 'sharp';
import type { Context } from '~/.server/context';
import { createFile } from '~/.server/storage';

export async function uploadAvatar(buffer: Buffer, context: Context) {
  const user = context.user.unwrap();
  const jpegBuffer = await sharp(buffer)
    .resize({
      width: 256,
      height: 256,
      fit: 'inside',
      withoutEnlargement: true,
    })
    .withMetadata()
    .jpeg({ quality: 80 })
    .toBuffer();
  await createFile(`user-data/${user.id}/avatar.jpg`, jpegBuffer, 'image/jpeg');
}
