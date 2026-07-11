import { describe, test, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { initialize, getPreviewUrls, revokePreviewUrl, dispose } from './FileUpload.razor.js';

/**
 * jsdom has no DataTransfer/FileList constructors, and `HTMLInputElement.files` is a native
 * getter-only property, so a real browser file selection can't be reproduced. Redefining `files`
 * as a plain writable own-property (shadowing the native accessor) lets tests assign arbitrary
 * file-like objects and drive the module's `change` handler exactly as a real selection would.
 */
function makeDropZoneAndInput(dropZoneId, inputId) {
    const dropZone = document.createElement('div');
    dropZone.id = dropZoneId;

    const input = document.createElement('input');
    input.type = 'file';
    input.id = inputId;
    Object.defineProperty(input, 'files', { value: [], writable: true, configurable: true });

    dropZone.appendChild(input);
    document.body.appendChild(dropZone);

    return { dropZone, input };
}

function makeFile(name, type) {
    return { name, type };
}

function selectFiles(input, files) {
    input.files = files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

describe('FileUpload', () => {
    let urlCounter;

    beforeEach(() => {
        document.body.innerHTML = '';
        urlCounter = 0;
        global.URL.createObjectURL = jest.fn(() => `blob:mock-url-${urlCounter++}`);
        global.URL.revokeObjectURL = jest.fn();
    });

    afterEach(() => {
        delete global.URL.createObjectURL;
        delete global.URL.revokeObjectURL;
    });

    describe('initialize', () => {
        test('should return without throwing when the drop zone element does not exist', () => {
            const input = document.createElement('input');
            input.id = 'missing-dz-input';
            document.body.appendChild(input);

            expect(() => initialize('missing-dropzone', 'missing-dz-input')).not.toThrow();
        });

        test('should return without throwing when the input element does not exist', () => {
            const dropZone = document.createElement('div');
            dropZone.id = 'dz-no-input';
            document.body.appendChild(dropZone);

            expect(() => initialize('dz-no-input', 'missing-input')).not.toThrow();
        });

        test('should reset preview state when initialized twice for the same drop zone', () => {
            const { input } = makeDropZoneAndInput('dz-reinit', 'input-reinit');
            initialize('dz-reinit', 'input-reinit');

            selectFiles(input, [makeFile('a.png', 'image/png')]);
            expect(getPreviewUrls('dz-reinit')).toEqual(['blob:mock-url-0']);

            initialize('dz-reinit', 'input-reinit');

            expect(getPreviewUrls('dz-reinit')).toEqual([]);
        });
    });

    describe('drag and drop visual state', () => {
        test('should add the active class and prevent default on dragover', () => {
            const { dropZone } = makeDropZoneAndInput('dz-1', 'input-1');
            initialize('dz-1', 'input-1');

            const event = new Event('dragover', { cancelable: true });
            dropZone.dispatchEvent(event);

            expect(dropZone.classList.contains('neba-file-upload-dropzone--active')).toBe(true);
            expect(event.defaultPrevented).toBe(true);
        });

        test('should remove the active class on dragleave', () => {
            const { dropZone } = makeDropZoneAndInput('dz-2', 'input-2');
            initialize('dz-2', 'input-2');

            dropZone.dispatchEvent(new Event('dragover', { cancelable: true }));
            dropZone.dispatchEvent(new Event('dragleave'));

            expect(dropZone.classList.contains('neba-file-upload-dropzone--active')).toBe(false);
        });
    });

    describe('drop', () => {
        test('should forward dropped files to the input and trigger preview generation', () => {
            const { dropZone } = makeDropZoneAndInput('dz-3', 'input-3');
            initialize('dz-3', 'input-3');

            const event = new Event('drop', { cancelable: true });
            event.dataTransfer = { files: [makeFile('dropped.png', 'image/png')] };
            dropZone.dispatchEvent(event);

            expect(getPreviewUrls('dz-3')).toEqual(['blob:mock-url-0']);
        });

        test('should prevent default and remove the active class on drop', () => {
            const { dropZone } = makeDropZoneAndInput('dz-4', 'input-4');
            initialize('dz-4', 'input-4');
            dropZone.dispatchEvent(new Event('dragover', { cancelable: true }));

            const event = new Event('drop', { cancelable: true });
            event.dataTransfer = { files: [makeFile('dropped.png', 'image/png')] };
            dropZone.dispatchEvent(event);

            expect(event.defaultPrevented).toBe(true);
            expect(dropZone.classList.contains('neba-file-upload-dropzone--active')).toBe(false);
        });

        test('should not throw and should leave previews unchanged when dropped with no files', () => {
            const { dropZone } = makeDropZoneAndInput('dz-5', 'input-5');
            initialize('dz-5', 'input-5');

            const event = new Event('drop', { cancelable: true });
            event.dataTransfer = { files: [] };

            expect(() => dropZone.dispatchEvent(event)).not.toThrow();
            expect(getPreviewUrls('dz-5')).toEqual([]);
        });

        test('should not throw when dropped with no dataTransfer at all', () => {
            const { dropZone } = makeDropZoneAndInput('dz-6', 'input-6');
            initialize('dz-6', 'input-6');

            const event = new Event('drop', { cancelable: true });

            expect(() => dropZone.dispatchEvent(event)).not.toThrow();
        });
    });

    describe('change / getPreviewUrls', () => {
        test('should generate a preview URL for image files and null for non-image files, in selection order', () => {
            const { input } = makeDropZoneAndInput('dz-7', 'input-7');
            initialize('dz-7', 'input-7');

            selectFiles(input, [
                makeFile('photo.png', 'image/png'),
                makeFile('document.pdf', 'application/pdf'),
            ]);

            expect(getPreviewUrls('dz-7')).toEqual(['blob:mock-url-0', null]);
        });

        test('should call URL.createObjectURL only for image files', () => {
            const { input } = makeDropZoneAndInput('dz-8', 'input-8');
            initialize('dz-8', 'input-8');

            selectFiles(input, [
                makeFile('photo.png', 'image/png'),
                makeFile('document.pdf', 'application/pdf'),
                makeFile('other.jpg', 'image/jpeg'),
            ]);

            expect(global.URL.createObjectURL).toHaveBeenCalledTimes(2);
        });

        test('should revoke previous preview URLs when a new selection is made', () => {
            const { input } = makeDropZoneAndInput('dz-9', 'input-9');
            initialize('dz-9', 'input-9');

            selectFiles(input, [makeFile('first.png', 'image/png')]);
            selectFiles(input, [makeFile('second.png', 'image/png')]);

            expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url-0');
            expect(getPreviewUrls('dz-9')).toEqual(['blob:mock-url-1']);
        });

        test('should return an empty array for a drop zone that was never initialized', () => {
            expect(getPreviewUrls('never-initialized')).toEqual([]);
        });
    });

    describe('revokePreviewUrl', () => {
        test('should call URL.revokeObjectURL with the given url', () => {
            revokePreviewUrl('blob:some-url');

            expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:some-url');
        });
    });

    describe('dispose', () => {
        test('should revoke all outstanding preview URLs for the drop zone', () => {
            const { input } = makeDropZoneAndInput('dz-10', 'input-10');
            initialize('dz-10', 'input-10');
            selectFiles(input, [makeFile('a.png', 'image/png'), makeFile('b.png', 'image/png')]);

            dispose('dz-10');

            expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url-0');
            expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url-1');
        });

        test('should clear tracked preview URLs so getPreviewUrls returns empty afterward', () => {
            const { input } = makeDropZoneAndInput('dz-11', 'input-11');
            initialize('dz-11', 'input-11');
            selectFiles(input, [makeFile('a.png', 'image/png')]);

            dispose('dz-11');

            expect(getPreviewUrls('dz-11')).toEqual([]);
        });

        test('should not throw when disposing a drop zone that was never initialized', () => {
            expect(() => dispose('never-initialized')).not.toThrow();
        });

        test('should not throw when called twice', () => {
            makeDropZoneAndInput('dz-12', 'input-12');
            initialize('dz-12', 'input-12');

            dispose('dz-12');

            expect(() => dispose('dz-12')).not.toThrow();
        });
    });

    describe('multi-instance isolation', () => {
        test('should keep two drop zones independent', () => {
            const { input: inputA } = makeDropZoneAndInput('dz-13', 'input-13');
            const { input: inputB } = makeDropZoneAndInput('dz-14', 'input-14');
            initialize('dz-13', 'input-13');
            initialize('dz-14', 'input-14');

            selectFiles(inputA, [makeFile('a.png', 'image/png')]);
            selectFiles(inputB, [makeFile('b.png', 'image/png')]);

            expect(getPreviewUrls('dz-13')).toEqual(['blob:mock-url-0']);
            expect(getPreviewUrls('dz-14')).toEqual(['blob:mock-url-1']);

            dispose('dz-13');

            expect(getPreviewUrls('dz-13')).toEqual([]);
            expect(getPreviewUrls('dz-14')).toEqual(['blob:mock-url-1']);
        });
    });
});
