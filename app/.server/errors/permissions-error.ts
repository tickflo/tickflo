import { ApiError } from './api-error';

export class PermissionsError extends ApiError {
  constructor(msg = 'You do not have permissions to do that action') {
    super(msg);
    this.code = 403;
    Object.setPrototypeOf(this, PermissionsError.prototype);
  }
}
