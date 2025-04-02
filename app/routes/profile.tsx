import { useEffect, useMemo, useRef, useState } from 'react';
import { FaEnvelope, FaSave, FaUndo } from 'react-icons/fa';
import { Form, data, useNavigation } from 'react-router';
import { getEmailChange, updateProfile } from '~/.server/services/user';
import { appContext } from '~/app-context';
import Countdown from '~/components/countdown';
import { ErrorAlert } from '~/components/error-alert';
import Toast from '~/components/toast';
import config from '~/config';
import type { Route } from './+types/profile';

export async function action({ request, context }: Route.ActionArgs) {
  const formData = await request.formData();
  const name = formData.get('name')?.toString();
  const email = formData.get('email')?.toString();
  const password = formData.get('password')?.toString();
  const newPassword = formData.get('new-password')?.toString();
  const confirmNewPassword = formData.get('confirm-new-password')?.toString();

  const ctx = context.get(appContext);

  const result = await updateProfile(
    {
      name,
      email,
      password,
      confirmNewPassword,
      newPassword,
    },
    ctx,
  );

  if (result.isErr()) {
    return data({ message: result.error.message });
  }

  return data({ message: null });
}

export async function loader({ context }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const user = ctx.user.unwrap();
  const emailChange = await getEmailChange(ctx);

  return data({
    user: {
      name: user.name,
      email: user.email,
    },
    emailChange: emailChange.isNone()
      ? null
      : {
          new: emailChange.value.new,
          createdAt: emailChange.value.createdAt,
          maxAge: emailChange.value.confirmMaxAge,
        },
  });
}

export default function Profile({
  actionData,
  loaderData,
}: Route.ComponentProps) {
  const errorMessage = useMemo(
    () => (actionData ? actionData.message : ''),
    [actionData],
  );
  const { user, emailChange } = loaderData;
  const [name, setName] = useState(user.name);
  const [email, setEmail] = useState(user.email);
  const [successMessage, setSuccessMessage] = useState('');

  const $form = useRef<HTMLFormElement>(null);
  const navigation = useNavigation();

  useEffect(
    function resetFormOnSuccess() {
      if (navigation.state === 'idle' && actionData && !errorMessage) {
        $form.current?.reset();
        setEmail(user.email);
        setSuccessMessage('Profile updated');
      }
    },
    [navigation.state, errorMessage, actionData, user.email],
  );

  return (
    <>
      {successMessage && (
        <Toast
          type="success"
          message={successMessage}
          onClose={() => setSuccessMessage('')}
        />
      )}
      <div className="flex min-h-screen flex-col items-center bg-base-200 pt-4">
        <div className="card w-full max-w-xl flex-shrink-0 bg-base-100 shadow-2xl">
          <div className="card-body">
            <h2 className="card-title">Profile</h2>
            <Form method="post" ref={$form}>
              <fieldset className="fieldset">
                <label htmlFor="name" className="fieldset-label">
                  Name
                </label>
                <input
                  id="name"
                  name="name"
                  type="text"
                  className="input"
                  placeholder="Name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  minLength={config.USER.MIN_NAME_LENGTH}
                  maxLength={config.USER.MAX_NAME_LENGTH}
                />

                <label htmlFor="email" className="fieldset-label">
                  Email
                </label>
                {!!emailChange && (
                  <div className="card w-full flex-shrink-0 bg-primary/10 shadow-2xl">
                    <div className="card-body">
                      <em>
                        A confirmation email has been sent to{' '}
                        <strong>{emailChange.new}</strong>.
                        <br />
                        You have{' '}
                        <strong>
                          <Countdown
                            createdAt={emailChange.createdAt}
                            maxAge={emailChange.maxAge}
                          />
                        </strong>{' '}
                        to confirm the change.
                      </em>
                      <div className="card-actions">
                        <Form method="post" action="/email-change/resend">
                          <button type="submit" className="btn btn-sm">
                            <FaEnvelope />
                            Resend
                          </button>
                        </Form>
                        <Form method="post" action="/email-change/resend">
                          <button type="submit" className="btn btn-sm">
                            <FaUndo />
                            Cancel
                          </button>
                        </Form>
                      </div>
                    </div>
                  </div>
                )}
                <input
                  id="email"
                  name="email"
                  type="email"
                  className="input"
                  placeholder="Email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
                <label htmlFor="new-password" className="fieldset-label">
                  New Password
                </label>
                <input
                  id="new-password"
                  name="new-password"
                  type="password"
                  className="input"
                  placeholder="Password"
                />
                <label
                  htmlFor="confirm-new-password"
                  className="fieldset-label"
                >
                  Confirm New Password
                </label>
                <input
                  id="confirm-new-password"
                  name="confirm-new-password"
                  type="password"
                  className="input"
                  placeholder="Confirm New Password"
                />
                <label htmlFor="password" className="fieldset-label">
                  Password
                </label>
                <input
                  id="password"
                  name="password"
                  type="password"
                  className="input"
                  placeholder="Password"
                />
              </fieldset>
              {errorMessage && <ErrorAlert message={errorMessage} />}
              <div className="mt-4">
                <button type="submit" className="btn btn-primary">
                  <FaSave /> Save Changes
                </button>
              </div>
            </Form>
          </div>
        </div>
      </div>
    </>
  );
}
