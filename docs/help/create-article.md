# Create an Article

Lets staff write and publish a news article for the site.

## Prerequisites

You need the `News.CreateArticle` permission (granted to the `Webmaster` and `Admin` roles), enforced via the dynamic `Permission:News.CreateArticle` policy — see the `Permission:{value}` row in [`docs/policies/README.md`](../policies/README.md). If you don't have it, no "Create Article" button appears on the News list page, and `/news/new` shows a "you don't have permission" message instead of the form.

## Steps

1. Go to **News** (`/news`).
2. Click the blue **+** button in the bottom-right corner of the page (the "Create Article" floating action button — this is the standard way to add a new item on any staff-managed list in the app).

   ![News list page with the Create Article floating action button visible](images/create-article/news-list-fab.png)

3. On the **Create Article** page, fill in:
   - **Title** — required.
   - **Slug** — optional. Leave it blank and the URL slug is generated from the title automatically — as you type the title, the Slug field's placeholder live-previews the slug that will be generated, e.g. "Season Champions & Records!! 2026" → `season-champions-records-2026`; type your own value only if you want to override that preview.
   - **Content** — required. Uses the rich text editor (bold/italic/lists/links/headings, etc.). You can also drag an image directly into the editor (or use its image button) to embed it inline in the article body — this uploads the image immediately and inserts it at the cursor.
   - **Tournament** — optional. Pick a season, then a tournament within that season, to link the article to it, or leave both as-is to publish with no tournament link. If you opened this page from a tournament's page (a "tournamentId" link), the tournament is already chosen and shown as read-only text instead of the dropdowns.
   - **Header Image** — optional. Drag and drop, or click to browse for, a single image (JPEG/PNG/etc., up to 5 MB) to use as the article's header image.
   - **Attachments** — optional. Drag and drop, or click to browse for, up to 10 files (PDF, Word, Excel, or images, up to 25 MB each) to attach to the article. Any image embedded inline in the Content editor also appears here, tagged **Inline**, alongside any files you add through this uploader.
   - **Status** — `Draft` (not visible to the public) or `Published`.
   - **Publish Date** — shown and entered in your own local time; it's converted to UTC automatically when the article is saved.

   ![Blank Create Article form](images/create-article/create-form.png)

   Once a header image or attachment finishes uploading, it's listed with a **Download**, **Open**, and **Remove** action. Removing a regular attachment just drops it from the list; removing an inline embedded image (used in the article body) asks you to confirm first, since the image will still appear — broken — wherever you embedded it in the content, and you'll need to edit the body yourself to fully remove or replace it.

   The **Create Article** button is disabled while any header image or attachment upload is still in progress, so you can't submit with a half-uploaded file.

4. Click **Create Article** to save, or **Cancel** to discard and return to the News list.

   If you've made any changes and try to leave the page before saving — via **Cancel**, clicking away to another page, or closing/refreshing the browser tab — you're asked to confirm first ("Discard unsaved changes?" with **Leave**/**Stay** options, or your browser's own "leave site?" prompt for a tab close/refresh). Choose **Stay** (or cancel the browser prompt) to keep working on the form.

## What happens after you confirm

- **Success**: an "Article Created" toast appears and you're taken straight to the new article's detail page (`/news/{slug}`).
- **Failure**: the form stays on screen with an "Unable to Create Article" message explaining what went wrong (see Troubleshooting below) — nothing is saved, and you can fix the form and try again without re-entering everything.

## Troubleshooting

| What you see | What it means |
| --- | --- |
| No "Create Article" button on the News list page, or a "you don't have permission" message on `/news/new` | You don't hold `News.CreateArticle` — ask an admin to grant it if you believe you should have it. |
| "An article with this slug already exists" | Another article is already using that slug (either auto-generated from a matching title, or a typed override). Change the title or supply a different slug and try again — this doesn't require a whole new submission of the rest of the form. |
| "Title must not be empty." / "Content must not be empty." | Title or Content was blank, or Content was entirely stripped during sanitization (e.g. it contained only disallowed markup like a bare script tag). Add real content and try again. |
| A red error status next to a file in the Header Image or Attachments list (e.g. "File exceeds the maximum allowed size of 5 MB." / "Upload failed.") | That individual file failed to upload — the rest of the form is unaffected. Remove it and try again with a smaller file or a different file. |
| "TournamentId must be a 26-character ULID." or a tournament-not-found error | The linked tournament no longer exists or wasn't recognized. Re-pick the season and tournament from the dropdowns and try again. |
| Any other "Unable to Create Article" message | The server rejected the request; the message describes the specific problem. The article was not created — correct the issue and submit again. |

## Related

- [`docs/policies/README.md`](../policies/README.md) — the `Permission:{value}` policy this action requires and how it's evaluated.
- [`docs/policies/can-manage-articles.md`](../policies/can-manage-articles.md) — a separate, broader policy that only controls status-badge visibility on News pages, not access to this form.
- [`docs/help/delete-article.md`](delete-article.md) — the equivalent doc for removing an article.
