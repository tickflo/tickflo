import { faker } from '@faker-js/faker';
import { Some } from 'ts-results-es';
import { expect, test } from 'vitest';
import { getTestContext } from '~/.server/context';
import { InputError } from '~/.server/errors';
import { slugify } from '~/utils/slugify';
import { getUserById } from '../user';
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
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);
  const email = faker.internet.email().toLowerCase();

  const { userId } = (
    await signup(
      {
        name: faker.person.firstName(),
        workspaceName,
        email,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId, slug }, context)).unwrap();

  const result = await confirmEmail(
    { code: 'invalid' },
    { ...context, user: Some(user) },
  );

  expect(result.isErr()).toBe(true);
});

test('Mark user confirmed with correct code', async () => {
  const context = await getTestContext();
  const email = faker.internet.email().toLowerCase();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { userId } = (
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

  let user = (await getUserById({ id: userId, slug }, context)).unwrap();

  (
    await confirmEmail(
      {
        code: user.emailConfirmationCode,
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  user = (await getUserById({ id: userId, slug }, context)).unwrap();

  expect(user.emailConfirmed).toBe(true);
  expect(user.emailConfirmationCode).toBeNull();
});

test('Throw on already confirmed', async () => {
  const context = await getTestContext();
  const email = faker.internet.email();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { userId } = (
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

  let user = (await getUserById({ id: userId, slug }, context)).unwrap();

  (
    await confirmEmail(
      {
        code: user.emailConfirmationCode,
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  user = (await getUserById({ id: userId, slug }, context)).unwrap();

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
