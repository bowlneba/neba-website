// FileUpload - drag-and-drop overlay + image thumbnail previews for the hidden <InputFile> that
// Blazor's InputFile component already wires up for click-to-browse and OnChange handling.
// This module never reads uploaded bytes itself; it only forwards a native `change` event so
// Blazor's own InputFile listener fires exactly as it would for a click-to-browse selection.

const instances = new Map(); // dropZoneId -> { input, previewUrls: (string|null)[] }

/**
 * Wires up drag-and-drop on the drop zone and change-triggered thumbnail preview generation on
 * the underlying file input.
 * @param {string} dropZoneId - The id of the drop zone container element.
 * @param {string} inputId - The id of the hidden <InputFile> element inside the drop zone.
 */
export function initialize(dropZoneId, inputId) {
    const dropZone = document.getElementById(dropZoneId);
    const input = document.getElementById(inputId);
    if (!dropZone || !input) {
        return;
    }

    if (instances.has(dropZoneId)) {
        dispose(dropZoneId);
    }

    const state = { input, previewUrls: [] };
    instances.set(dropZoneId, state);

    const highlight = () => dropZone.classList.add('neba-file-upload-dropzone--active');
    const unhighlight = () => dropZone.classList.remove('neba-file-upload-dropzone--active');

    dropZone.addEventListener('dragover', (event) => {
        event.preventDefault();
        highlight();
    });

    dropZone.addEventListener('dragleave', unhighlight);

    dropZone.addEventListener('drop', (event) => {
        event.preventDefault();
        unhighlight();

        if (!event.dataTransfer?.files?.length) {
            return;
        }

        // InputFile has no native drop support, so a drop is turned into the same `change` event
        // a click-to-browse selection would fire, letting Blazor's own listener take it from here.
        input.files = event.dataTransfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });

    input.addEventListener('change', () => {
        revokePreviewUrls(state);

        state.previewUrls = Array.from(input.files ?? []).map((file) =>
            file.type.startsWith('image/') ? URL.createObjectURL(file) : null);
    });
}

/**
 * Returns the preview URLs generated for the input's current file selection, in the same order
 * .NET's `InputFileChangeEventArgs.GetMultipleFiles()` will enumerate them.
 * @param {string} dropZoneId - The id of the drop zone container element.
 * @returns {(string|null)[]} One entry per selected file; null for non-image files.
 */
export function getPreviewUrls(dropZoneId) {
    return instances.get(dropZoneId)?.previewUrls ?? [];
}

/**
 * Revokes a single preview URL immediately, used when a file is individually removed before the
 * whole component is disposed.
 * @param {string} url - The object URL to revoke.
 */
export function revokePreviewUrl(url) {
    URL.revokeObjectURL(url);
}

function revokePreviewUrls(state) {
    for (const url of state.previewUrls) {
        if (url) {
            URL.revokeObjectURL(url);
        }
    }

    state.previewUrls = [];
}

/**
 * Tears down the drop zone's listeners and revokes any outstanding preview URLs.
 * @param {string} dropZoneId - The id of the drop zone container element.
 */
export function dispose(dropZoneId) {
    const state = instances.get(dropZoneId);
    if (!state) {
        return;
    }

    revokePreviewUrls(state);
    instances.delete(dropZoneId);
}
