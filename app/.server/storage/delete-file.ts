import { DeleteObjectCommand } from '@aws-sdk/client-s3';
import { s3 } from '.';
import config from '../config';

export async function deleteFile(path: string) {
  const command = new DeleteObjectCommand({
    Bucket: config.STORAGE.S3_BUCKET,
    Key: `${path}`,
  });

  await s3.send(command);
}
