export interface UserConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
  CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES: number;
  CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES: number;
}

export interface LocationConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
}

export interface ContactConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
}

export interface WorkspaceConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
  MAX_SLUG_LENGTH: number;
}

export interface PortalConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
  MAX_SLUG_LENGTH: number;
  MIN_SECTION_TITLE_LENGTH: number;
  MAX_SECTION_TITLE_LENGTH: number;
  MIN_QUESTION_LABEL_LENGTH: number;
  MAX_QUESTION_LABEL_LENGTH: number;
}

export interface RoleConfig {
  MIN_NAME_LENGTH: number;
  MAX_NAME_LENGTH: number;
}

interface AppConfig {
  USER: UserConfig;
  CONTACT: ContactConfig;
  LOCATION: LocationConfig;
  ROLE: RoleConfig;
  WORKSPACE: WorkspaceConfig;
  PORTAL: PortalConfig;
}

const config = {
  USER: {
    MIN_NAME_LENGTH: 2,
    MAX_NAME_LENGTH: 100,
    CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES: 30,
    CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES: 24 * 60,
  },
  CONTACT: {
    MIN_NAME_LENGTH: 2,
    MAX_NAME_LENGTH: 100,
  },
  WORKSPACE: {
    MIN_NAME_LENGTH: 3,
    MAX_NAME_LENGTH: 100,
    MAX_SLUG_LENGTH: 30,
  },
  LOCATION: {
    MIN_NAME_LENGTH: 2,
    MAX_NAME_LENGTH: 100,
  },
  PORTAL: {
    MIN_NAME_LENGTH: 3,
    MAX_NAME_LENGTH: 100,
    MAX_SLUG_LENGTH: 30,
    MIN_SECTION_TITLE_LENGTH: 3,
    MAX_SECTION_TITLE_LENGTH: 100,
    MIN_QUESTION_LABEL_LENGTH: 3,
    MAX_QUESTION_LABEL_LENGTH: 300,
  },
  ROLE: {
    MIN_NAME_LENGTH: 3,
    MAX_NAME_LENGTH: 30,
  },
} as AppConfig;

export default config;
