import { GetObjectCommand } from '@aws-sdk/client-s3';
import { None, type Option, Some } from 'ts-results-es';
import { s3 } from '.';
import config from '../config';

type File = {
  contentType: string | undefined;
  buffer: Buffer;
};

export async function getFile(path: string): Promise<Option<File>> {
  const command = new GetObjectCommand({
    Bucket: config.STORAGE.S3_BUCKET,
    Key: `${path}`,
  });

  try {
    const result = await s3.send(command);
    if (!result.ContentType || !result.Body) {
      return None;
    }

    return Some({
      contentType: result.ContentType,
      buffer: (await result.Body.transformToByteArray()) as Buffer,
    });
  } catch (err) {
    console.error('Get file error', err);
    return None;
  }
}
