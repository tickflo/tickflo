import { S3Client } from '@aws-sdk/client-s3';
import config from '../config';

export const s3 = new S3Client({
  region: config.STORAGE.S3_REGION,
  endpoint: config.STORAGE.S3_ENDPOINT,
  credentials: {
    accessKeyId: config.STORAGE.S3_ACCESS_KEY,
    secretAccessKey: config.STORAGE.S3_SECRET_KEY,
  },
  forcePathStyle: true,
});

export * from './create-file';
export * from './create-folder';
export * from './get-file';
export * from './delete-file';
