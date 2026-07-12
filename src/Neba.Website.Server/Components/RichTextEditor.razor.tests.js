import { describe, test, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { initialize, getHtml, setHtml, setReadOnly, dispose } from './RichTextEditor.razor.js';

class MockQuill {
    constructor(container, options) {
        this.container = container;
        this.options = options;
        this.root = document.createElement('div');
        this.root.className = 'ql-editor';
        this.listeners = {};
        this.enabled = true;
        this.clipboard = {
            dangerouslyPasteHTML: (html) => {
                this.root.innerHTML = html;
            }
        };

        // Mimic real Quill DOM behavior: a toolbar is inserted as a sibling *before* the
        // container, and the container itself hosts `.root` (`.ql-editor`). This lets tests
        // catch the duplicate-toolbar bug that plain internal-state mocking would hide.
        this.toolbarElement = document.createElement('div');
        this.toolbarElement.className = 'ql-toolbar';
        // Mirrors the real toolbar DOM shape Quill renders for our custom `modules.toolbar`
        // config, so tests can verify tooltip attributes land on the right elements.
        this.toolbarElement.innerHTML = `
            <span class="ql-formats"><span class="ql-picker ql-header"><span class="ql-picker-label"></span></span></span>
            <span class="ql-formats">
                <button class="ql-bold"></button>
                <button class="ql-italic"></button>
                <button class="ql-underline"></button>
            </span>
            <span class="ql-formats"><span class="ql-picker ql-color"><span class="ql-picker-label"></span></span></span>
            <span class="ql-formats"><button class="ql-link"></button></span>
            <span class="ql-formats">
                <button class="ql-list" value="ordered"></button>
                <button class="ql-list" value="bullet"></button>
            </span>
            <span class="ql-formats"><button class="ql-image"></button></span>
            <span class="ql-formats"><button class="ql-clean"></button></span>
        `;
        container.parentElement?.insertBefore(this.toolbarElement, container);
        container.classList.add('ql-container', 'ql-snow');
        container.appendChild(this.root);

        this.toolbarHandlers = {};

        MockQuill.instances.push(this);
    }

    getModule(name) {
        if (name !== 'toolbar') {
            return null;
        }

        return {
            container: this.toolbarElement,
            addHandler: (name, fn) => {
                this.toolbarHandlers[name] = fn;
            }
        };
    }

    getSelection() {
        return { index: 0 };
    }

    on(event, callback) {
        this.listeners[event] = callback;
    }

    off(event) {
        delete this.listeners[event];
    }

    enable(state) {
        this.enabled = state;
    }

    emit(event, ...args) {
        this.listeners[event]?.(...args);
    }
}

MockQuill.instances = [];

function makeContainer(id) {
    const container = document.createElement('div');
    container.id = id;
    document.body.appendChild(container);
    return container;
}

function makeDotNetRef() {
    return { invokeMethodAsync: jest.fn() };
}

/** Returns the most recently constructed MockQuill instance. */
function lastQuillInstance() {
    return MockQuill.instances.at(-1);
}

describe('RichTextEditor', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        MockQuill.instances = [];
        globalThis.Quill = MockQuill;
    });

    afterEach(() => {
        delete globalThis.Quill;
    });

    describe('initialize', () => {
        test('should create a Quill instance bound to the given container', async () => {
            const container = makeContainer('rte-1');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-1', dotNetRef, '', false, 'Write here...');

            expect(lastQuillInstance().container).toBe(container);
            expect(lastQuillInstance().options).toMatchObject({ theme: 'snow', readOnly: false, placeholder: 'Write here...' });
        });

        test('should load initial HTML content when provided', async () => {
            makeContainer('rte-2');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-2', dotNetRef, '<p>Hello</p>', false, '');

            expect(getHtml('rte-2')).toBe('<p>Hello</p>');
        });

        test('should initialize as read-only when requested', async () => {
            makeContainer('rte-2b');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-2b', dotNetRef, '', true, '');

            expect(lastQuillInstance().options.readOnly).toBe(true);
        });

        test('should return early without throwing when the container does not exist', async () => {
            const dotNetRef = makeDotNetRef();

            await expect(initialize('missing-container', dotNetRef, '', false, '')).resolves.toBeUndefined();
        });

        test('should replace an existing instance when initialized twice for the same container', async () => {
            makeContainer('rte-3');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-3', dotNetRef, '<p>First</p>', false, '');
            const firstInstance = lastQuillInstance();
            await initialize('rte-3', dotNetRef, '<p>Second</p>', false, '');

            expect(getHtml('rte-3')).toBe('<p>Second</p>');
            expect(firstInstance.listeners['text-change']).toBeUndefined();
        });

        test('should not leave a duplicate toolbar when initialized twice for the same container', async () => {
            makeContainer('rte-3b');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-3b', dotNetRef, '<p>First</p>', false, '');
            await initialize('rte-3b', dotNetRef, '<p>Second</p>', false, '');

            expect(document.querySelectorAll('.ql-toolbar').length).toBe(1);
        });

        test('should remove a stray leftover toolbar not tracked by the instance map before creating a new one', async () => {
            // Simulates Blazor reusing a DOM node whose JS-inserted content (toolbar,
            // editor markup) survives a re-render without this module's dispose() running —
            // the actual root cause of the duplicate-toolbar bug this test guards against.
            const container = makeContainer('rte-3c');
            const staleToolbar = document.createElement('div');
            staleToolbar.className = 'ql-toolbar';
            container.parentElement.insertBefore(staleToolbar, container);
            container.classList.add('ql-container', 'ql-snow');
            container.innerHTML = '<div class="ql-editor">stale</div>';

            const dotNetRef = makeDotNetRef();
            await initialize('rte-3c', dotNetRef, '<p>Fresh</p>', false, '');

            expect(document.querySelectorAll('.ql-toolbar').length).toBe(1);
            expect(getHtml('rte-3c')).toBe('<p>Fresh</p>');
        });
    });

    describe('toolbar tooltips', () => {
        test('should set a descriptive title attribute on every toolbar control', async () => {
            makeContainer('rte-3d');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-3d', dotNetRef, '', false, '');

            const toolbar = lastQuillInstance().toolbarElement;
            expect(toolbar.querySelector('.ql-header .ql-picker-label').getAttribute('title')).toBe('Heading style');
            expect(toolbar.querySelector('button.ql-bold').getAttribute('title')).toBe('Bold');
            expect(toolbar.querySelector('button.ql-italic').getAttribute('title')).toBe('Italic');
            expect(toolbar.querySelector('button.ql-underline').getAttribute('title')).toBe('Underline');
            expect(toolbar.querySelector('.ql-color .ql-picker-label').getAttribute('title')).toBe('Text color');
            expect(toolbar.querySelector('button.ql-link').getAttribute('title')).toBe('Insert link');
            expect(toolbar.querySelector('button.ql-list[value="ordered"]').getAttribute('title')).toBe('Numbered list');
            expect(toolbar.querySelector('button.ql-list[value="bullet"]').getAttribute('title')).toBe('Bulleted list');
            expect(toolbar.querySelector('button.ql-image').getAttribute('title')).toBe('Insert image');
            expect(toolbar.querySelector('button.ql-clean').getAttribute('title')).toBe('Remove formatting');
        });

        test('should not throw when the toolbar module is unavailable', async () => {
            makeContainer('rte-3e');
            const dotNetRef = makeDotNetRef();

            await expect(initialize('rte-3e', dotNetRef, '', false, '')).resolves.toBeUndefined();
        });
    });

    describe('getHtml', () => {
        test('should return an empty string when the container was never initialized', () => {
            expect(getHtml('never-initialized')).toBe('');
        });
    });

    describe('setHtml', () => {
        test('should replace editor content and getHtml should reflect the new value', async () => {
            makeContainer('rte-4');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-4', dotNetRef, '<p>Original</p>', false, '');

            setHtml('rte-4', '<p>Updated</p>');

            expect(getHtml('rte-4')).toBe('<p>Updated</p>');
        });

        test('should not throw when called for an uninitialized container', () => {
            expect(() => setHtml('never-initialized', '<p>x</p>')).not.toThrow();
        });
    });

    describe('setReadOnly', () => {
        test('should disable the editor when readOnly is true', async () => {
            makeContainer('rte-5');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-5', dotNetRef, '', false, '');

            setReadOnly('rte-5', true);

            expect(lastQuillInstance().enabled).toBe(false);
        });

        test('should re-enable the editor when readOnly is false', async () => {
            makeContainer('rte-5b');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-5b', dotNetRef, '', true, '');

            setReadOnly('rte-5b', false);

            expect(lastQuillInstance().enabled).toBe(true);
        });

        test('should not throw when called for an uninitialized container', () => {
            expect(() => setReadOnly('never-initialized', true)).not.toThrow();
        });
    });

    describe('text-change notifications', () => {
        test('should invoke NotifyContentChanged with the updated HTML when the change is user-sourced', async () => {
            makeContainer('rte-6');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-6', dotNetRef, '', false, '');

            const quill = lastQuillInstance();
            quill.root.innerHTML = '<p>Typed by user</p>';
            quill.emit('text-change', null, null, 'user');

            expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyContentChanged', '<p>Typed by user</p>');
        });

        test('should ignore text-change events not sourced from the user', async () => {
            makeContainer('rte-7');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-7', dotNetRef, '', false, '');

            const quill = lastQuillInstance();
            quill.root.innerHTML = '<p>Programmatic change</p>';
            quill.emit('text-change', null, null, 'api');

            expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });
    });

    describe('dispose', () => {
        test('should remove the instance so getHtml returns empty afterward', async () => {
            makeContainer('rte-9');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-9', dotNetRef, '<p>Content</p>', false, '');

            dispose('rte-9');

            expect(getHtml('rte-9')).toBe('');
        });

        test('should remove the text-change listener on dispose', async () => {
            makeContainer('rte-9b');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-9b', dotNetRef, '', false, '');
            const quill = lastQuillInstance();

            dispose('rte-9b');
            quill.emit('text-change', null, null, 'user');

            expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should not throw when called twice', async () => {
            makeContainer('rte-10');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-10', dotNetRef, '', false, '');

            dispose('rte-10');

            expect(() => dispose('rte-10')).not.toThrow();
        });

        test('should not throw when getHtml/setHtml are called after dispose', async () => {
            makeContainer('rte-11');
            const dotNetRef = makeDotNetRef();
            await initialize('rte-11', dotNetRef, '', false, '');

            dispose('rte-11');

            expect(() => getHtml('rte-11')).not.toThrow();
            expect(() => setHtml('rte-11', '<p>x</p>')).not.toThrow();
        });

        test('should not throw when disposing a container that was never initialized', () => {
            expect(() => dispose('never-initialized')).not.toThrow();
        });
    });

    describe('image embedding', () => {
        beforeEach(() => {
            globalThis.DotNet = { createJSStreamReference: jest.fn((file) => ({ file })) };
        });

        afterEach(() => {
            delete globalThis.DotNet;
        });

        test('should register a custom image handler when hasImageHandler is true', async () => {
            makeContainer('rte-img-1');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-img-1', dotNetRef, '', false, '', true);

            const quill = lastQuillInstance();
            expect(typeof quill.toolbarHandlers.image).toBe('function');
            expect(quill.options.modules.toolbar.handlers.image).toBeNull();
        });

        test('should not register an image handler when hasImageHandler is false', async () => {
            makeContainer('rte-img-2');
            const dotNetRef = makeDotNetRef();

            await initialize('rte-img-2', dotNetRef, '', false, '', false);

            const quill = lastQuillInstance();
            expect(quill.toolbarHandlers.image).toBeUndefined();
            expect(Array.isArray(quill.options.modules.toolbar)).toBe(true);
        });

        test('should upload the picked file and embed the returned URL at the saved selection', async () => {
            makeContainer('rte-img-3');
            const dotNetRef = makeDotNetRef();
            dotNetRef.invokeMethodAsync.mockResolvedValue('https://example.com/photo.png');

            await initialize('rte-img-3', dotNetRef, '', false, '', true);
            const quill = lastQuillInstance();
            quill.getSelection = jest.fn(() => ({ index: 4 }));
            quill.insertEmbed = jest.fn();
            quill.setSelection = jest.fn();

            let capturedInput;
            const originalCreateElement = document.createElement.bind(document);
            jest.spyOn(document, 'createElement').mockImplementation((tag) => {
                const element = originalCreateElement(tag);
                if (tag === 'input') {
                    capturedInput = element;
                }
                return element;
            });

            quill.toolbarHandlers.image();

            const file = new File(['data'], 'photo.png', { type: 'image/png' });
            Object.defineProperty(capturedInput, 'files', { value: [file] });
            capturedInput.dispatchEvent(new Event('change'));

            // Flush the async handler chain (file-picked callback -> invokeMethodAsync -> insertEmbed).
            await Promise.resolve();
            await Promise.resolve();
            await Promise.resolve();

            expect(globalThis.DotNet.createJSStreamReference).toHaveBeenCalledWith(file);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
                'RequestImageEmbedAsync', expect.anything(), 'photo.png', 'image/png');
            expect(quill.insertEmbed).toHaveBeenCalledWith(4, 'image', 'https://example.com/photo.png', 'user');
            expect(quill.setSelection).toHaveBeenCalledWith(5, 0, 'user');

            document.createElement.mockRestore();
        });

        test('should not embed anything when the caller returns no URL (e.g. upload failed)', async () => {
            makeContainer('rte-img-4');
            const dotNetRef = makeDotNetRef();
            dotNetRef.invokeMethodAsync.mockResolvedValue(null);

            await initialize('rte-img-4', dotNetRef, '', false, '', true);
            const quill = lastQuillInstance();
            quill.getSelection = jest.fn(() => ({ index: 2 }));
            quill.insertEmbed = jest.fn();
            quill.setSelection = jest.fn();

            let capturedInput;
            const originalCreateElement = document.createElement.bind(document);
            jest.spyOn(document, 'createElement').mockImplementation((tag) => {
                const element = originalCreateElement(tag);
                if (tag === 'input') {
                    capturedInput = element;
                }
                return element;
            });

            quill.toolbarHandlers.image();

            const file = new File(['data'], 'photo.png', { type: 'image/png' });
            Object.defineProperty(capturedInput, 'files', { value: [file] });
            capturedInput.dispatchEvent(new Event('change'));

            await Promise.resolve();
            await Promise.resolve();
            await Promise.resolve();

            expect(quill.insertEmbed).not.toHaveBeenCalled();
            expect(quill.setSelection).not.toHaveBeenCalled();

            document.createElement.mockRestore();
        });
    });

    describe('multi-instance isolation', () => {
        test('should keep two editor instances independent', async () => {
            makeContainer('rte-12');
            makeContainer('rte-13');
            const dotNetRefA = makeDotNetRef();
            const dotNetRefB = makeDotNetRef();

            await initialize('rte-12', dotNetRefA, '<p>A</p>', false, '');
            await initialize('rte-13', dotNetRefB, '<p>B</p>', false, '');

            expect(getHtml('rte-12')).toBe('<p>A</p>');
            expect(getHtml('rte-13')).toBe('<p>B</p>');

            dispose('rte-12');

            expect(getHtml('rte-12')).toBe('');
            expect(getHtml('rte-13')).toBe('<p>B</p>');
        });
    });
});
