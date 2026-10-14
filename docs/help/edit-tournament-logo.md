# Edit a Tournament's Logo

Lets staff replace or remove a tournament's logo without being able to change anything else about the tournament.

## Prerequisites

You need the `Tournaments.EditTournamentLogo` permission. `Tournaments.EditTournament` alone is not enough, so an admin can deny logo changes to a user who can still edit the rest of a tournament. If you lack the permission, no edit-logo button appears.

The Edit Tournament form treats a missing permission and a denial differently. If your role simply does not grant `Tournaments.EditTournamentLogo`, you can still change the logo there, because a logo is part of the tournament. If an admin denied the permission to your account, the form shows the logo read only, other changes still save, and the API refuses a logo change. Creating a tournament is not affected: you can still upload a logo when you create one.

`Tournaments.EditTournamentLogo` is held by the Admin, Webmaster, Manager, and Tournament Director roles. An admin can also grant it to a single user through that user's permission overrides.

## Steps

1. Find the tournament's logo on either page:
   - the **tournament detail page** (`/tournaments/{id}`) — the **Edit logo** button sits on the logo in the top section;
   - the **Tournaments** schedule (`/tournaments`) — a small pencil button sits on the logo of the next-tournament banner and each tournament card.
2. Click the button. The **Edit logo** window opens with the tournament's name and its current logo.
3. Do one of:
   - **Replace it.** Drag an image onto the upload area, or click to browse (one image, up to 5 MB). The preview switches to the new logo and is marked **New logo**. Choose **Discard new logo** to go back to the current one.
   - **Remove it.** Click **Remove current logo**. The tournament then shows its title sponsor's logo if one is assigned, or a default logo based on the tournament type.
4. Click **Save logo**. The window closes and a "Logo Saved" message appears. The page shows the change right away.

**Cancel**, **Escape**, or a click outside the window closes it. If you have an unsaved change, you're asked to confirm first ("Discard unsaved changes?").

## Troubleshooting

| What you see | What it means |
| --- | --- |
| No edit-logo button | You hold neither `Tournaments.EditTournamentLogo` nor `Tournaments.EditTournament`. Ask an admin to grant one. |
| **Save logo** is greyed out | Nothing has changed yet, or an upload is still running. |
| A red status next to the upload (e.g. "File exceeds the maximum allowed size of 5 MB.") | The file did not upload. Try a smaller or different image. |
| "Couldn't Save Logo" with a message | The server did not save the change; the message says why. The tournament's logo is unchanged — fix the issue and save again. |
| "This tournament could not be found." | Someone deleted the tournament. Return to the Tournaments list. |

## Related

- [`docs/help/edit-tournament.md`](edit-tournament.md) — the full tournament edit, which also covers the logo.
- [`docs/policies/README.md`](../policies/README.md) — the `CanUploadTournamentLogo` policy.
