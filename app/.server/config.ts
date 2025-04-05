import 'dotenv/config';
import type {
  ContactConfig,
  LocationConfig,
  PortalConfig,
  RoleConfig,
  UserConfig,
  WorkspaceConfig,
} from '~/config';
import appConfig from '~/config';

interface Config {
  POSTGRES_USER: string;
  POSTGRES_PASSWORD: string;
  POSTGRES_DB: string;
  POSTGRES_HOST: string;
  SESSION_TIMEOUT_MINUTES: number;
  BASE_URL: string;
  USER: UserConfig;
  CONTACT: ContactConfig;
  LOCATION: LocationConfig;
  ROLE: RoleConfig;
  WORKSPACE: WorkspaceConfig;
  PORTAL: PortalConfig;
  STORAGE: StorageConfig;
}

interface StorageConfig {
  S3_ENDPOINT: string;
  S3_ACCESS_KEY: string;
  S3_SECRET_KEY: string;
  S3_BUCKET: string;
  S3_REGION: string;
}

const config = {
  POSTGRES_USER: process.env.POSTGRES_USER || 'tickflo',
  POSTGRES_PASSWORD: process.env.POSTGRES_PASSWORD || 'password',
  POSTGRES_DB: process.env.POSTGRES_DB || 'tickflo',
  POSTGRES_HOST: process.env.POSTGRES_HOST || 'localhost',
  SESSION_TIMEOUT_MINUTES: 20,
  BASE_URL: process.env.BASE_URL || 'http://localhost:3000',
  STORAGE: {
    S3_ENDPOINT: process.env.S3_ENDPOINT || 'http://localhost:9000',
    S3_ACCESS_KEY: process.env.S3_ACCESS_KEY || 'tickflo',
    S3_SECRET_KEY: process.env.S3_SECRET_KEY || 'password',
    S3_BUCKET: process.env.S3_BUCKET || 'tickflo',
    S3_REGION: process.env.S3_REGION || 'us-east-1',
  },
  ...appConfig,
} as Config;

export default config;
