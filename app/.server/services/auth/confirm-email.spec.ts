import { faker } from '@faker-js/faker';
import { Some } from 'ts-results-es';
import { expect, test } from 'vitest';
import { getTestContext } from '~/.server/context';
import { InputError } from '~/.server/errors';
import { getUserForAccessToken } from '../user';
import { confirmEmail } from './confirm-email';
import { signup } from './signup';

const PASSWORD = 'password';

test('Throw on missing code', async () => {
  const context = await getTestContext();
  const result = await confirmEmail(
    {
      code: '',
    },
    context,
  );
  expect(result.isErr()).toBe(true);
});

test('Throw on invalid code', async () => {
  const context = await getTestContext();
  const email = faker.internet.email().toLowerCase();

  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        workspaceName: faker.company.name(),
        email,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const user = (await getUserForAccessToken({ token }, context)).unwrap();

  const result = await confirmEmail(
    { code: 'invalid' },
    { ...context, user: Some(user) },
  );

  expect(result.isErr()).toBe(true);
});

test('Mark user confirmed with correct code', async () => {
  const context = await getTestContext();
  const email = faker.internet.email().toLowerCase();

  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        workspaceName: faker.company.name(),
        email,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  let user = (await getUserForAccessToken({ token }, context)).unwrap();

  (
    await confirmEmail(
      {
        code: user.emailConfirmationCode,
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  user = (await getUserForAccessToken({ token }, context)).unwrap();

  expect(user.emailConfirmed).toBe(true);
  expect(user.emailConfirmationCode).toBeNull();
});

test('Throw on already confirmed', async () => {
  const context = await getTestContext();
  const email = faker.internet.email();

  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        workspaceName: faker.company.name(),
        email,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  let user = (await getUserForAccessToken({ token }, context)).unwrap();

  (
    await confirmEmail(
      {
        code: user.emailConfirmationCode,
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  user = (await getUserForAccessToken({ token }, context)).unwrap();

  const error = (
    await confirmEmail(
      {
        code: user.emailConfirmationCode,
      },
      { ...context, user: Some(user) },
    )
  ).unwrapErr();

  expect(error).toBeInstanceOf(InputError);
});
