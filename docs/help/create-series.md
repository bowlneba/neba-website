# Create a Series

Lets staff set up a Vincent Hall Memorial Series for a season: pick its tournaments, give each one a role, and, if the Series has one, schedule its Finale.

## Prerequisites

You need the `Series.CreateSeries` permission, enforced through the dynamic `Permission:Series.CreateSeries` policy. See the `Permission:{value}` row in [`docs/policies/README.md`](../policies/README.md). The Admin and Manager roles hold it. If you don't have it, no **+** button appears on the Series list page, and `/series/new` shows a "you don't have permission" message instead of the form.

Create the season's tournaments first. A Stage can only use a tournament that already exists, belongs to the Series' season, and is not already a Stage in another Series.

## Steps

1. Go to **Tournaments**, then **Series** (`/series`).
2. Click the blue **+** button in the bottom-right corner.
3. On the **Create Series** page, fill in:
   - **Name** (required), up to 127 characters, for example "2027 Vincent Hall Memorial Series".
   - **Season** (required). You cannot change it after you save. To move a Series to another season, delete it and create it again.
   - **Logo** (optional). Drag an image onto the upload area, or click to browse (one image, up to 5 MB). Without a logo, the pages show the Series' initials.
   - **Stages**. Click **Add stage** for each tournament in the Series. For each row choose:
     - **Kind**: **Qualifying**, **Survivor** or **Placement**. Qualifying Stages come first, then Survivor, then at most one Placement, which must be last.
     - **Tournament**. The list shows the season's tournaments that are not already in another Series.
     - **Advancing count**. Required on Qualifying and Survivor rows, and it must be above zero. For a team event the hint reads, for example, "Teams of 3", with "(6 bowlers)" below it. A Placement row has no count. Each Survivor count must be smaller than the one before it.
     - **Squad date** and **start time** on Survivor rows only. Describe the squad the way bowlers say it, such as "9:00 AM on 7/12". The date must fall within the tournament's dates.

     Use the arrows to move a row up or down. Stages must follow tournament start dates, so the order has to match the calendar.
   - **Finale** (optional). Turn on **This Series has a Finale**, then enter the number of **finalists**, the **date**, the **start time**, and, if you know it, the **host center**. Leave the host blank until it is announced. The finalist count must be above zero and no larger than the last Survivor Stage's count. Every Stage's tournament must end before the Finale date.
4. Click **Create Series** to save, or **Cancel** to discard and return to the list.

You can save a Series that is not finished, for example with no Placement Stage yet. Add the rest later with **Edit**.

## What happens after you save

- **Success**: a "Series Created" message appears and you go to the new Series' page.
- **A rule is broken**: the form stays open and each message appears next to the row or field it names, for example "Stage 3 (Westgate Lanes): a Placement Stage has no advancing count." Fix them and save again. Nothing is saved until every message is cleared.
- **A tournament is already in another Series**: the row shows which Series holds it. Remove the row or pick another tournament.

If you try to leave the page with unsaved changes, you are asked to confirm first.

## Troubleshooting

| What you see | What it means |
| --- | --- |
| No **+** button on the Series list | You don't hold `Series.CreateSeries`. Ask an admin to grant it. |
| A tournament is missing from the picker | It is in another season, or it is already a Stage in another Series. |
| "a Survivor Stage needs a squad date and start time" | Fill in both on that Survivor row. |
| "No Stage can come after the Finale" | A Stage's tournament ends on or after the Finale date. Move the Finale later or remove the Stage. |
| "The selected Finale bowling center does not exist or is not open" | Choose an open center, or leave the host blank. |
| The logo shows a red status | The file did not upload. Try a smaller or different image. |

## Related

- [`docs/help/edit-series.md`](edit-series.md): change a Series after it is created.
- [`docs/help/delete-series.md`](delete-series.md): remove a Series.
- [`docs/help/edit-series-logo.md`](edit-series-logo.md): change only the logo.
- [`docs/help/create-tournament.md`](create-tournament.md): create the tournaments a Series uses.
- [`docs/policies/README.md`](../policies/README.md): the `Series.CreateSeries` and `CanUploadSeriesLogo` policies.
