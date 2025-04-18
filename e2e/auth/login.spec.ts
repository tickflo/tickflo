import { faker } from '@faker-js/faker';
import { expect, test } from '@playwright/test';
import { Some } from 'ts-results-es';
import { getTestContext } from '~/.server/context';
import { signup } from '~/.server/services/auth';
import { getRoles } from '~/.server/services/security';
import { getUserForAccessToken } from '~/.server/services/user';
import { addUser, removeUser } from '~/.server/services/user';
import { createWorkspace } from '~/.server/services/workspace';
import { slugify } from '~/utils/slugify';

const PASSWORD = 'password';

test.beforeEach(async ({ page }) => {
  await page.goto('./login');
});

test('Redirects to workspace page after successful login', async ({ page }) => {
  const workspaceName = faker.company.name();
  const email = faker.internet.email();
  const slug = slugify(workspaceName);

  const context = await getTestContext();
  (
    await signup(
      {
        name: faker.person.firstName(),
        email,
        recoveryEmail: faker.internet.email(),
        workspaceName,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  await page.getByLabel('Email').fill(email);
  await page.locator('input[name="password"]').fill(PASSWORD);
  await page.getByRole('button', { name: 'Login' }).click();

  await page.waitForURL(`**/workspaces/${slug}`);

  const linkTitle = await page
    .getByRole('link', { name: workspaceName })
    .textContent();

  expect(linkTitle).toBe(workspaceName);
});

test('Redirects to workspace picker for more than one workspace', async ({
  page,
}) => {
  const email = faker.internet.email();
  const context = await getTestContext();
  const workspaceNames = [faker.company.name(), faker.company.name()];
  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        email,
        recoveryEmail: faker.internet.email(),
        workspaceName: workspaceNames[0],
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const user = (await getUserForAccessToken({ token }, context)).unwrap();

  (
    await createWorkspace(
      { name: workspaceNames[1] },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  await page.getByLabel('Email').fill(email);
  await page.locator('input[name="password"]').fill(PASSWORD);
  await page.getByRole('button', { name: 'Login' }).click();

  await page.waitForURL('**/workspaces');

  await expect(page.getByText(workspaceNames[0])).toBeVisible();
  await expect(page.getByText(workspaceNames[1])).toBeVisible();
});

test('Redirects to workspace picker for one workspace and an invite', async ({
  page,
}) => {
  const email = faker.internet.email();
  const context = await getTestContext();
  const workspaceNames = [faker.company.name(), faker.company.name()];
  (
    await signup(
      {
        name: faker.person.firstName(),
        email,
        recoveryEmail: faker.internet.email(),
        workspaceName: workspaceNames[0],
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        recoveryEmail: faker.internet.email(),
        workspaceName: workspaceNames[1],
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const slug = slugify(workspaceNames[1]);
  const user = (await getUserForAccessToken({ token }, context)).unwrap();

  const roles = (
    await getRoles({ slug }, { ...context, user: Some(user) })
  ).unwrap();

  (
    await addUser(
      {
        slug,
        email,
        name: faker.person.firstName(),
        roleIds: [roles[0].id],
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  await page.getByLabel('Email').fill(email);
  await page.locator('input[name="password"]').fill(PASSWORD);
  await page.getByRole('button', { name: 'Login' }).click();

  await page.waitForURL('**/workspaces');

  await expect(page.getByText(workspaceNames[0])).toBeVisible();
  await expect(page.getByText(workspaceNames[1])).toBeVisible();
  await expect(page.getByText('Invites')).toBeVisible();
});

test('Redirect to create workspace for no workspaces', async ({ page }) => {
  const email = faker.internet.email();
  const context = await getTestContext();
  const workspaceName = faker.company.name();
  const slug = slugify(workspaceName);

  const { token } = (
    await signup(
      {
        name: faker.person.firstName(),
        email: faker.internet.email(),
        recoveryEmail: faker.internet.email(),
        workspaceName: workspaceName,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  const user = (await getUserForAccessToken({ token }, context)).unwrap();

  const roles = (
    await getRoles({ slug }, { ...context, user: Some(user) })
  ).unwrap();

  (
    await addUser(
      {
        slug,
        email,
        name: faker.person.firstName(),
        roleIds: [roles[0].id],
      },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  const { userId: removeUserId } = (
    await signup(
      {
        name: faker.person.firstName(),
        email,
        recoveryEmail: faker.internet.email(),
        workspaceName: undefined,
        password: PASSWORD,
        confirmPassword: PASSWORD,
      },
      context,
    )
  ).unwrap();

  (
    await removeUser(
      { userId: removeUserId, slug },
      { ...context, user: Some(user) },
    )
  ).unwrap();

  await page.getByLabel('Email').fill(email);
  await page.locator('input[name="password"]').fill(PASSWORD);
  await page.getByRole('button', { name: 'Login' }).click();

  await page.waitForURL('**/workspaces/new');
  await expect(page.getByText('New Workspace')).toBeVisible();
});

test('Error on missing email', async ({ page }) => {
  await page.locator('input[name="password"]').fill(PASSWORD);
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page.getByText('Email is required')).toBeVisible();
});

test('Error on missing password', async ({ page }) => {
  await page.getByLabel('Email').fill(faker.internet.email());
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page.getByText('Password is required')).toBeVisible();
});
