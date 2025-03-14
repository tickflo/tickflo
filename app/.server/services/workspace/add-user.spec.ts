import { faker } from '@faker-js/faker';
import { eq } from 'drizzle-orm';
import { Some } from 'ts-results-es';
import { expect, test } from 'vitest';
import { getTestContext } from '~/.server/context';
import { emailTemplates } from '~/.server/data';
import { InputError } from '~/.server/errors';
import { slugify } from '~/utils/slugify';
import { db } from '../../db';
import { emails } from '../../db/schema';
import { signup } from '../auth';
import { getUserByEmail, getUserById } from '../user';
import { addUser } from './add-user';
import { getRoles } from './get-roles';

test('Throws on invalid name', async () => {
  const context = await getTestContext();
  const { config } = context;

  const { userId } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        workspaceName: faker.company.name(),
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();

  expect(
    (
      await addUser(
        {
          slug: 'foo-bar',
          roleIds: [1],
          email: faker.internet.email(),
          name: undefined,
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);

  expect(
    (
      await addUser(
        {
          slug: 'foo-bar',
          roleIds: [1],
          email: faker.internet.email(),
          name: faker.string.alpha({ length: config.USER.MIN_NAME_LENGTH - 1 }),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);

  expect(
    (
      await addUser(
        {
          slug: 'foo-bar',
          roleIds: [1],
          email: faker.internet.email(),
          name: faker.string.alpha({ length: config.USER.MAX_NAME_LENGTH + 1 }),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Throws on invalid email', async () => {
  const context = await getTestContext();
  const { userId } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        workspaceName: faker.company.name(),
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();
  expect(
    (
      await addUser(
        {
          slug: 'foo-bar',
          roleIds: [1],
          email: undefined,
          name: faker.person.firstName(),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Throws on invalid roleId', async () => {
  const context = await getTestContext();
  const { userId } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        workspaceName: faker.company.name(),
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();
  expect(
    (
      await addUser(
        {
          slug: 'foo-bar',
          roleIds: [Number.NaN],
          email: faker.internet.email(),
          name: faker.person.firstName(),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Throws on non-existent role', async () => {
  const context = await getTestContext();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { userId } = (
    await signup(
      {
        email: faker.internet.email(),
        workspaceName,
        name: faker.person.firstName(),
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();

  expect(
    (
      await addUser(
        {
          slug,
          roleIds: [99],
          email: faker.internet.email(),
          name: faker.person.firstName(),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Throw on non-existent workspace', async () => {
  const context = await getTestContext();
  const { userId } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        workspaceName: faker.company.name(),
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();

  expect(
    (
      await addUser(
        {
          slug: slugify(faker.company.name()),
          roleIds: [1],
          email: faker.internet.email(),
          name: faker.person.firstName(),
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Throw on adding existing member', async () => {
  const context = await getTestContext();
  const email = faker.internet.email();
  const name = faker.person.firstName();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { userId } = (
    await signup(
      {
        email,
        name,
        workspaceName,
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();

  const roles = await getRoles({ slug }, { ...context, user: Some(user) });

  expect(
    (
      await addUser(
        {
          slug,
          roleIds: [roles[0]?.id],
          email,
          name,
        },
        { ...context, user: Some(user) },
      )
    ).unwrapErr(),
  ).toBeInstanceOf(InputError);
});

test('Add new user', async () => {
  const context = await getTestContext();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { userId } = (
    await signup(
      {
        email: faker.internet.email(),
        name: faker.person.firstName(),
        workspaceName,
        password: 'password',
        confirmPassword: 'password',
      },
      context,
    )
  ).unwrap();

  const user = (await getUserById({ id: userId }, context)).unwrap();

  const roles = (
    await getRoles({ slug }, { ...context, user: Some(user) })
  ).unwrap();

  const name = faker.person.firstName();
  const email = faker.internet.email();

  (
    await addUser(
      {
        slug,
        roleIds: [roles[0]?.id],
        email,
        name,
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  const newUser = (await getUserByEmail({ email }, context)).unwrap();

  expect(newUser.name).toBe(name);

  const emailRecord = await db.query.emails.findFirst({
    where: eq(emails.to, email.toLowerCase()),
    with: {
      template: true,
    },
  });

  expect(emailRecord?.template.templateTypeId).toBe(
    emailTemplates.existingWorkspaceMemberInvitation.typeId,
  );
});
