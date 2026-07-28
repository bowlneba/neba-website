# Reset a User's Password

Lets an admin force a password reset for an existing staff account — the user gets an emailed link and sets a brand-new password themselves. No temporary or admin-visible password is ever generated.

## API only — no UI yet

This action is currently only reachable by calling the API directly (e.g. via Swagger or a REST client) — there is no page in the app for it yet. This doc covers the endpoint's behavior and permissions now so it's ready to expand with UI steps and screenshots once a page is built.

## Prerequisites

You need the `System.ResetUserPassword` permission, enforced via the dynamic `Permission:System.ResetUserPassword` policy — see the `Permission:{value}` row in [`docs/policies/README.md`](../policies/README.md).

## What it does

`POST /security/password/reset` with a `{ "userId": "<ulid>" }` body:

1. Looks up the user by id. If no such user exists, the request fails (404) — nothing is sent.
2. Generates a one-time password-reset token and emails the user a **Set Your Password** link (subject: "Your BowlNEBA password has been reset").
3. The user's existing password stays valid until they actually complete the link — this call alone does not lock them out.

This is the same set-password mechanism used for [inviting a new user](create-user.md#setting-the-password-invite-flow): the link opens `/account/set-password` with the user's id and token embedded in the URL, requires no login to use, and is one-time — using it (or letting it expire) invalidates it for further use.

## Response

- **204 No Content** — the reset email was sent.
- **404 Not Found** — no user exists with the given id.
- **422 Unprocessable Entity** — the request body failed validation.

## Troubleshooting

| What you see | What it means |
| --- | --- |
| 401/403 | You don't hold `System.ResetUserPassword` — ask an admin to grant it if you believe you should have it. |
| 404 | The `userId` doesn't match any existing account. Double-check the id. |
| The user says the link doesn't work | Reset links are one-time and expire — if the user waited too long or the link was already used (e.g. requested twice), call the endpoint again to send a fresh one. |

## Related

- [Create a User](create-user.md) — the invite flow shares the same set-password link mechanism.
- [`docs/policies/README.md`](../policies/README.md) — the `Permission:{value}` policy this action requires and how it's evaluated.
