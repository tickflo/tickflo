import {
  Form,
  Link,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  data,
  isRouteErrorResponse,
  useRouteLoaderData,
} from 'react-router';

import { useState } from 'react';
import {
  FaBell,
  FaEnvelope,
  FaMoon,
  FaSignOutAlt,
  FaSun,
  FaUser,
} from 'react-icons/fa';
import { FaPeopleGroup, FaScrewdriverWrench } from 'react-icons/fa6';
import Icon from '~/tickflo-icon.png';
import Logo from '~/tickflo.png';
import type { Route } from './+types/root';
import { getContext } from './.server/context';
import { loginRedirect } from './.server/helpers';
import { commitSession } from './.server/session';
import { appContext } from './app-context';
import stylesheet from './app.css?url';
import { EmailConfirmationAlert } from './components/email-confirmation-alert';
import Toast from './components/toast';

export const links: Route.LinksFunction = () => [
  { rel: 'stylesheet', href: stylesheet },
];

function isAuthRequired(url: URL): boolean {
  return ![
    '/login',
    '/signup',
    '/api/system/reset-db',
    '/email-change/undo',
  ].includes(url.pathname);
}

const auth: Route.unstable_MiddlewareFunction = async (
  { context, request },
  next,
) => {
  const url = new URL(request.url);
  const ctx = await getContext(request);
  if (isAuthRequired(url) && ctx.user.isNone()) {
    if (url.pathname === '/') {
      throw await loginRedirect(ctx.session, request.url, '');
    }

    throw await loginRedirect(ctx.session, request.url);
  }

  context.set(appContext, ctx);

  await next();
};

export const unstable_middleware = [auth];

export async function loader({ context }: Route.LoaderArgs) {
  const ctx = context.get(appContext);
  const { session, preferences, user } = ctx;

  return data(
    {
      error: session.get('error'),
      message: session.get('message'),
      theme: preferences.get('theme') || 'light',
      dismissedEmailConfirmation:
        session.get('dismissedEmailConfirmation') || false,
      user: user.isSome()
        ? {
            id: user.value.id,
            name: user.value.name,
            email: user.value.email,
            emailConfirmed: user.value.emailConfirmed,
            systemAdmin: user.value.systemAdmin,
          }
        : null,
    },
    {
      headers: {
        'Set-Cookie': await commitSession(session),
      },
    },
  );
}

type RootLoaderData = typeof loader;

export function Layout({ children }: { children: React.ReactNode }) {
  const data = useRouteLoaderData<RootLoaderData | undefined>('root');
  const user = data?.user;
  const [errorToast, setErrorToast] = useState(data?.error);
  const [messageToast, setMessageToast] = useState(data?.message);

  return (
    <html lang="en" data-theme={data?.theme || 'light'}>
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="icon" href={Icon} />
        <Meta />
        <Links />
      </head>
      <body>
        {user && !user.emailConfirmed && !data?.dismissedEmailConfirmation && (
          <EmailConfirmationAlert />
        )}
        <div className="navbar bg-base-100 shadow-sm">
          <div className="flex-1">
            <Link className="btn btn-ghost text-xl" to="/">
              <img src={Logo} alt="Tickflo" className="h-12" />
            </Link>
          </div>
          <div className="flex-none">
            <ul className="menu menu-horizontal px-1">
              <li>
                <Form method="post" action="/toggle-theme">
                  <button type="submit" className="pt-1">
                    {data?.theme === 'dark' ? (
                      <FaMoon className="cursor-pointer" />
                    ) : (
                      <FaSun className="cursor-pointer" />
                    )}
                  </button>
                </Form>
              </li>
              <li>
                <Link to="/notifications">
                  <FaBell className="mt-1 inline" />
                  <span className="badge badge-xs badge-primary">99+</span>
                </Link>
              </li>
              {!user && (
                <li>
                  <Link to="/login">Login</Link>
                </li>
              )}
              {user && (
                <>
                  {user.systemAdmin && (
                    <li>
                      <div className="dropdown dropdown-center">
                        {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                        <div tabIndex={0} role="button">
                          <FaScrewdriverWrench className="inline" /> System
                        </div>
                        <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                          <li>
                            <Link
                              to="/sys/emails"
                              onClick={(e) => e.currentTarget.blur()}
                            >
                              <FaEnvelope /> Emails
                            </Link>
                          </li>
                          <li>
                            <Link
                              to="/sys/workspaces"
                              onClick={(e) => e.currentTarget.blur()}
                            >
                              <FaPeopleGroup /> Workspaces
                            </Link>
                          </li>
                          <li>
                            <Link
                              to="/sys/users"
                              onClick={(e) => e.currentTarget.blur()}
                            >
                              <FaPeopleGroup /> Users
                            </Link>
                          </li>
                        </ul>
                      </div>
                    </li>
                  )}
                  <li>
                    <div className="dropdown dropdown-end">
                      {/*biome-ignore lint/a11y/useSemanticElements: reason required for safari*/}
                      <div tabIndex={0} role="button">
                        <div className="avatar mr-4">
                          <div className="w-8 rounded-full ring ring-offset-2 ring-offset-base-100">
                            <img
                              src={`/users/${user.id}/avatar`}
                              alt="Avatar Preview"
                            />
                          </div>
                        </div>
                        {user.email}
                      </div>
                      <ul className="dropdown-content menu z-1 w-52 rounded-box bg-base-100 p-2 shadow-sm">
                        <li>
                          <Link
                            to="/profile"
                            onClick={(e) => e.currentTarget.blur()}
                          >
                            <FaUser /> Profile
                          </Link>
                        </li>
                        <li>
                          <Link
                            to="/workspaces"
                            onClick={(e) => e.currentTarget.blur()}
                          >
                            <FaPeopleGroup /> Workspaces
                          </Link>
                        </li>
                        <li>
                          <form method="post" action="/logout">
                            <button type="submit" className="cursor-pointer">
                              <FaSignOutAlt className="mr-2 inline" />
                              Logout
                            </button>
                          </form>
                        </li>
                      </ul>
                    </div>
                  </li>
                </>
              )}
            </ul>
          </div>
        </div>
        {errorToast && (
          <Toast
            type="error"
            message={errorToast}
            onClose={() => setErrorToast('')}
          />
        )}
        {messageToast && (
          <Toast
            type="info"
            message={messageToast}
            onClose={() => setMessageToast('')}
          />
        )}
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}

export default function App() {
  return <Outlet />;
}

export function ErrorBoundary({ error }: Route.ErrorBoundaryProps) {
  let message = 'Oops!';
  let details = 'An unexpected error occurred.';
  let stack: string | undefined;

  if (isRouteErrorResponse(error)) {
    message = error.status === 404 ? '404' : 'Error';
    details =
      error.status === 404
        ? 'The requested page could not be found.'
        : error.statusText || details;
  } else if (import.meta.env.DEV && error && error instanceof Error) {
    details = error.message;
    stack = error.stack;
  }

  return (
    <main className="container mx-auto p-4 pt-16">
      <h1>{message}</h1>
      <p>{details}</p>
      {stack && (
        <pre className="w-full overflow-x-auto p-4">
          <code>{stack}</code>
        </pre>
      )}
    </main>
  );
}
