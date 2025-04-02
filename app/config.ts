export interface UserConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
  CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES: number;
  CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES: number;
}

export interface WorkspaceConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
  MAX_SLUG_LENGTH: number;
}

export interface RoleConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
}

interface AppConfig {
  USER: UserConfig;
  ROLE: RoleConfig;
  WORKSPACE: WorkspaceConfig;
}

const config = {
  USER: {
    MIN_NAME_LENGTH: 2,
    MAX_NAME_LENGTH: 100,
    CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES: 30,
    CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES: 24 * 60,
  },
  WORKSPACE: {
    MIN_NAME_LENGTH: 3,
    MAX_NAME_LENGTH: 100,
    MAX_SLUG_LENGTH: 30,
  },
  ROLE: {
    MIN_NAME_LENGTH: 3,
    MAX_NAME_LENGTH: 30,
  },
} as AppConfig;

export default config;
