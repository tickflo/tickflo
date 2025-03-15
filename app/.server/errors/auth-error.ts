import { ApiError } from './api-error';

export class AuthError extends ApiError {
  constructor(msg = 'Invalid username or password, please try again') {
    super(msg);
    this.code = 401;
    Object.setPrototypeOf(this, AuthError.prototype);
  }
}
