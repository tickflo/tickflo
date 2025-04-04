import { PutObjectCommand } from '@aws-sdk/client-s3';
import { s3 } from '.';
import config from '../config';

export async function createFolder(path: string) {
  const command = new PutObjectCommand({
    Bucket: config.STORAGE.S3_BUCKET,
    Key: `${path}/`,
    Body: '',
  });

  await s3.send(command);
}
