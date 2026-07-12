// RichTextEditor - Wraps Quill for rich text editing within a Blazor component
// Note: Assumes 'Quill' is available globally from the Quill CDN script tag (see App.razor)

const instances = new Map(); // containerId -> { quill, dotNetRef }

/**
 * Waits for the Quill library to be loaded
 * @returns {Promise} Promise that resolves when Quill is available
 */
function waitForQuill() {
    return new Promise((resolve) => {
        if (typeof Quill !== 'undefined') {
            resolve();
            return;
        }

        const checkQuill = setInterval(() => {
            if (typeof Quill !== 'undefined') {
                clearInterval(checkQuill);
                resolve();
            }
        }, 100);

        setTimeout(() => {
            clearInterval(checkQuill);
            if (typeof Quill === 'undefined') {
                console.error('[RichTextEditor] Quill failed to load within timeout');
            }
        }, 10000);
    });
}

// Maps toolbar control selectors to the tooltip text shown on hover.
// Keep in sync with the `modules.toolbar` array passed to `new Quill(...)` below.
const toolbarTooltips = [
    ['.ql-header .ql-picker-label', 'Heading style'],
    ['button.ql-bold', 'Bold'],
    ['button.ql-italic', 'Italic'],
    ['button.ql-underline', 'Underline'],
    ['.ql-color .ql-picker-label', 'Text color'],
    ['button.ql-link', 'Insert link'],
    ['button.ql-list[value="ordered"]', 'Numbered list'],
    ['button.ql-list[value="bullet"]', 'Bulleted list'],
    ['button.ql-image', 'Insert image'],
    ['button.ql-clean', 'Remove formatting']
];

/**
 * Opens a native file picker restricted to images, and hands the picked file to `onFilePicked`.
 * @param {(file: File) => void} onFilePicked - Called once with the picked file, if any.
 */
function pickImageFile(onFilePicked) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';

    input.addEventListener('change', () => {
        const file = input.files && input.files[0];
        if (file) {
            onFilePicked(file);
        }
    });

    input.click();
}

/**
 * Quill toolbar handler for the image control: picks an image file, streams it to .NET via
 * `RequestImageEmbedAsync` (which uploads it and returns a displayable URL), then embeds the
 * returned URL at the selection that was active when the control was clicked. Quill loses the
 * selection once the (async, out-of-band) file picker/upload completes, so the range is captured
 * up front and restored before inserting.
 * @param {any} quill - The Quill instance.
 * @param {any} dotNetRef - DotNet object reference to invoke back into .NET.
 */
function createImageHandler(quill, dotNetRef) {
    return function handleImageClick() {
        const range = quill.getSelection(true);

        pickImageFile(async (file) => {
            const streamRef = DotNet.createJSStreamReference(file);

            const url = await dotNetRef.invokeMethodAsync(
                'RequestImageEmbedAsync', streamRef, file.name, file.type);

            if (url) {
                quill.insertEmbed(range.index, 'image', url, 'user');
                quill.setSelection(range.index + 1, 0, 'user');
            }
        });
    };
}

/**
 * Sets a native `title` attribute on each toolbar control so hovering shows what it does.
 * @param {HTMLElement | null | undefined} toolbarElement - The toolbar's container element.
 */
function applyToolbarTooltips(toolbarElement) {
    if (!toolbarElement) {
        return;
    }

    for (const [selector, tooltip] of toolbarTooltips) {
        toolbarElement.querySelectorAll(selector).forEach((element) => {
            element.setAttribute('title', tooltip);
        });
    }
}

/**
 * Initializes a Quill editor instance bound to the given container.
 * @param {string} containerId - The id of the element Quill should render into.
 * @param {any} dotNetRef - DotNet object reference used to notify content changes.
 * @param {string} initialHtml - The initial HTML content to load into the editor.
 * @param {boolean} readOnly - Whether the editor should start in read-only mode.
 * @param {string} placeholder - Placeholder text shown when the editor is empty.
 * @param {boolean} hasImageHandler - Whether the caller wired up `OnImageSelected`. When false, the
 * toolbar's image control is omitted entirely rather than rendered disabled.
 */
export async function initialize(containerId, dotNetRef, initialHtml, readOnly, placeholder, hasImageHandler) {
    await waitForQuill();

    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    if (instances.has(containerId)) {
        dispose(containerId);
    }

    // Defensive: Quill's toolbar is inserted as a DOM sibling *before* the container,
    // outside anything Blazor's renderer tracks. If this container was ever initialized
    // before (e.g. Blazor reused the element across a re-render without this module's
    // dispose() running first), strip any leftover toolbar/content so we don't end up
    // with duplicate toolbars stacking up.
    const previousToolbar = container.previousElementSibling;
    if (previousToolbar?.classList.contains('ql-toolbar')) {
        previousToolbar.remove();
    }
    container.innerHTML = '';
    container.classList.remove('ql-container', 'ql-snow');

    const toolbar = [
        [{ header: [1, 2, 3, false] }],
        ['bold', 'italic', 'underline'],
        [{ color: [] }],
        ['link'],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ...(hasImageHandler ? [['image']] : []),
        ['clean']
    ];

    const quill = new Quill(container, {
        theme: 'snow',
        readOnly: !!readOnly,
        placeholder: placeholder || '',
        modules: {
            toolbar: hasImageHandler
                ? { container: toolbar, handlers: { image: null } }
                : toolbar
        }
    });

    if (hasImageHandler) {
        // Quill's default image handler base64-encodes the file inline — replaced with our own
        // upload-then-embed flow (see createImageHandler) after construction, since the handler
        // needs a reference to the constructed `quill` instance.
        quill.getModule('toolbar').addHandler('image', createImageHandler(quill, dotNetRef));
    }

    applyToolbarTooltips(quill.getModule('toolbar')?.container);

    if (initialHtml) {
        quill.clipboard.dangerouslyPasteHTML(initialHtml);
    }

    instances.set(containerId, { quill, dotNetRef });

    quill.on('text-change', (_delta, _oldDelta, source) => {
        if (source !== 'user') {
            return;
        }

        dotNetRef.invokeMethodAsync('NotifyContentChanged', quill.root.innerHTML);
    });
}

/**
 * Gets the current HTML content of the editor.
 * @param {string} containerId - The id of the editor's container.
 * @returns {string} The current HTML content, or an empty string if not initialized.
 */
export function getHtml(containerId) {
    const state = instances.get(containerId);
    return state ? state.quill.root.innerHTML : '';
}

/**
 * Replaces the editor's content with the given HTML.
 * @param {string} containerId - The id of the editor's container.
 * @param {string} html - The HTML content to load into the editor.
 */
export function setHtml(containerId, html) {
    const state = instances.get(containerId);
    if (!state) {
        return;
    }

    state.quill.clipboard.dangerouslyPasteHTML(html || '');
}

/**
 * Enables or disables editing on the editor instance.
 * @param {string} containerId - The id of the editor's container.
 * @param {boolean} readOnly - True to disable editing, false to enable it.
 */
export function setReadOnly(containerId, readOnly) {
    const state = instances.get(containerId);
    if (!state) {
        return;
    }

    state.quill.enable(!readOnly);
}

/**
 * Tears down the Quill instance and removes its listeners for the given container.
 * @param {string} containerId - The id of the editor's container.
 */
export function dispose(containerId) {
    const state = instances.get(containerId);
    if (!state) {
        return;
    }

    state.quill.off('text-change');

    // Quill inserts its toolbar as a sibling before the container, not as a child of it —
    // removing it explicitly is required, since it otherwise survives the container's own removal/reset.
    const toolbar = state.quill.getModule('toolbar')?.container;
    toolbar?.remove();

    instances.delete(containerId);
}
