// Called from a Blazor circuit (SessionCookieSync) to read or rewrite the auth cookie, which the
// circuit can't do itself. Blobs are encrypted server-side; this file never sees raw tokens.
// redirect: 'manual' turns the cookie handler's login redirect into a failed response.
const endpoint = '/account/session-tokens';

export async function persist(blob) {
  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      credentials: 'same-origin',
      redirect: 'manual',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ blob }),
    });
    return response.ok;
  } catch (error) {
    console.warn('Could not update the session cookie.', error);
    return false;
  }
}

export async function read() {
  try {
    const response = await fetch(endpoint, {
      credentials: 'same-origin',
      redirect: 'manual',
      cache: 'no-store',
    });
    if (response.status !== 200) {
      return null;
    }
    const body = await response.json();
    return typeof body?.blob === 'string' ? body.blob : null;
  } catch (error) {
    console.warn('Could not read the session cookie.', error);
    return null;
  }
}
