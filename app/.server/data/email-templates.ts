/*
 * All email templates are defined here.
 *
 * Don't forget to add new templates to the emailTemplates export
 * and either the systemEmailTemplates or workspaceEmailTemplates export!
 */

export type EmailTemplate = {
  typeId: number;
  subject: string;
  body: string;
};

const signup = {
  typeId: 1,
  subject: 'Welcome to Tickflo! Confirm Your Email',
  body: `Hello,

Thank you for signing up! Please confirm your email address by clicking the link below:

{{confirmation_link}}

If you did not sign up, you can ignore this email.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

const forgotPassword = {
  typeId: 2,
  subject: 'Reset Your Password',
  body: `Hello,

We received a request to reset your password. Click the link below to set a new password:

{{reset_link}}

If you did not request this, you can ignore this email.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

const confirmNewEmail = {
  typeId: 3,
  subject: 'Confirm your new email address',
  body: `Hello,

We received a request to change your email address. Please confirm this change by clicking the link below:

{{confirmation_link}}

If you did not request this, please ignore this email.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

const revertEmailChange = {
  typeId: 4,
  subject: 'Your email address was changed',
  body: `Hello,

Your Tickflo account email was changed to {{new_email}}. If you made this change, no further action is needed.

If you did NOT request this change, you have until {{expires_at}} to undo this change by clicking the link below:

{{revert_link}}

After this period, you will need to contact support.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

// NOTE: Workspace typeIds start with 101 to leave room for new system templates

const existingWorkspaceMemberInvitation = {
  typeId: 101,
  subject: 'You’re Invited! Join Our Workspace',
  body: `Hello {{name}},

You’ve been invited to join {{workspace_name}}. Simply login and click accept to join {{workspace_name}}:

{{login_link}}

If you weren’t expecting this invitation, you can ignore this email.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

const newWorkspaceMemberInvitation = {
  typeId: 102,
  subject: 'You’re Invited! Join Our Workspace',
  body: `Hello {{name}},

You’ve been invited to join {{workspace_name}}. Click the link below to create your account and get started:

{{signup_link}}

If you weren’t expecting this invitation, you can ignore this email.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

const workspaceMemberRemoval = {
  typeId: 103,
  subject: 'Your access to {{workspace_name}} has been removed',
  body: `Hello {{name}},

You’ve been removed from {{workspace_name}}. 

Contact your administrator if you belive this is a mistake.

Best regards,  
Tickflo Team`,
} satisfies EmailTemplate;

export const emailTemplates = {
  signup,
  forgotPassword,
  newWorkspaceMemberInvitation,
  existingWorkspaceMemberInvitation,
  workspaceMemberRemoval,
  confirmNewEmail,
  revertEmailChange,
};

export const systemEmailTemplates = [
  signup,
  forgotPassword,
  confirmNewEmail,
  revertEmailChange,
];

export const workspaceEmailTemplates = [
  newWorkspaceMemberInvitation,
  existingWorkspaceMemberInvitation,
  workspaceMemberRemoval,
];
