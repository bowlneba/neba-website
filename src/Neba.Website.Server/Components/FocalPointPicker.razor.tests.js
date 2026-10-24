import { describe, test, expect, beforeEach, afterEach, jest } from '@jest/globals';
import {
    clamp,
    cropSize,
    centerOf,
    positionFor,
    imageStyle,
    initialize,
    update,
    dispose,
} from './FocalPointPicker.razor.js';

describe('FocalPointPicker math', () => {
    describe('imageStyle', () => {
        test('should match the style FocalPoint.ToImageStyle renders', () => {
            expect(imageStyle(31, 36, 150)).toBe('object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.5)');
            expect(imageStyle(50, 50, 100)).toBe('object-position:50% 50%;transform-origin:50% 50%;transform:scale(1)');
        });
    });

    describe('clamp', () => {
        test('should limit values to 0 to 100', () => {
            expect(clamp(-5)).toBe(0);
            expect(clamp(105)).toBe(100);
            expect(clamp(42)).toBe(42);
        });
    });

    describe('cropSize', () => {
        test('should cover the full height of a landscape photo in a round shape', () => {
            const size = cropSize(4 / 3, 1, 100);

            expect(size.cw).toBeCloseTo(0.75);
            expect(size.ch).toBeCloseTo(1);
        });

        test('should cover the full width of a portrait photo in a round shape', () => {
            const size = cropSize(3 / 4, 1, 100);

            expect(size.cw).toBeCloseTo(1);
            expect(size.ch).toBeCloseTo(0.75);
        });

        test('should shrink the window as zoom grows', () => {
            const size = cropSize(4 / 3, 1, 200);

            expect(size.cw).toBeCloseTo(0.375);
            expect(size.ch).toBeCloseTo(0.5);
        });

        test('should use the shape aspect ratio for a rectangle', () => {
            const size = cropSize(1, 2, 100);

            expect(size.cw).toBeCloseTo(1);
            expect(size.ch).toBeCloseTo(0.5);
        });
    });

    describe('positionFor', () => {
        test('should stop at the photo edge when the point is too near it', () => {
            expect(positionFor(0.3, 0.5, cropSize(4 / 3, 1, 100)).x).toBe(0);
        });

        test('should reach a point near the edge once zoomed in', () => {
            const size = cropSize(4 / 3, 1, 200);
            const { x } = positionFor(0.31, 0.5, size);

            expect(centerOf(x, 50, size).cx).toBeCloseTo(0.31, 1);
        });

        test('should keep an axis with no overflow at 50', () => {
            expect(positionFor(0.5, 0.1, cropSize(4 / 3, 1, 100)).y).toBe(50);
        });

        test('should clamp to 0 and 100', () => {
            const size = cropSize(4 / 3, 1, 100);

            expect(positionFor(-1, 0.5, size).x).toBe(0);
            expect(positionFor(2, 0.5, size).x).toBe(100);
        });
    });

    describe('centerOf', () => {
        test('should round-trip with positionFor inside the reachable range', () => {
            const size = cropSize(16 / 9, 1, 150);
            const { cx, cy } = centerOf(40, 60, size);

            expect(positionFor(cx, cy, size)).toEqual({ x: 40, y: 60 });
        });
    });
});

describe('FocalPointPicker module', () => {
    const rootId = 'picker-test';
    let helper;
    let surface;
    let crop;
    let slider;
    let image;

    function render(shape = 'circle', aspect = '1') {
        document.body.innerHTML = `
            <div id="${rootId}" class="focal-picker" data-shape="${shape}" data-aspect="${aspect}">
                <div data-fp-surface tabindex="0">
                    <img />
                    <span data-fp-crop></span>
                    <span data-fp-marker></span>
                </div>
                <img data-fp-preview />
                <input type="range" data-fp-zoom min="100" max="300" value="100" />
                <output data-fp-zoom-text>1.0×</output>
            </div>`;
        surface = document.querySelector('[data-fp-surface]');
        crop = document.querySelector('[data-fp-crop]');
        slider = document.querySelector('[data-fp-zoom]');
        image = surface.querySelector('img');
        surface.getBoundingClientRect = () => ({ left: 0, top: 0, width: 400, height: 300 });
    }

    function loadImage(width, height) {
        Object.defineProperty(image, 'naturalWidth', { value: width, configurable: true });
        Object.defineProperty(image, 'naturalHeight', { value: height, configurable: true });
        image.dispatchEvent(new Event('load'));
    }

    function pointer(type, fx, fy) {
        surface.dispatchEvent(new MouseEvent(type, { clientX: fx * 400, clientY: fy * 300, button: 0, bubbles: true }));
    }

    function key(name, shiftKey = false) {
        surface.dispatchEvent(new KeyboardEvent('keydown', { key: name, shiftKey, bubbles: true, cancelable: true }));
    }

    function flush() {
        jest.advanceTimersByTime(50);
    }

    function lastNotified() {
        const calls = helper.invokeMethodAsync.mock.calls;
        return calls[calls.length - 1].slice(1);
    }

    beforeEach(() => {
        jest.useFakeTimers();
        helper = { invokeMethodAsync: jest.fn() };
        render();
    });

    afterEach(() => {
        dispose(rootId);
        jest.useRealTimers();
    });

    test('should size and place the crop outline from the saved framing', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        expect(crop.style.width).toBe('75%');
        expect(crop.style.height).toBe('100%');
        expect(crop.style.left).toBe('50%');
        expect(crop.style.top).toBe('50%');
        expect(crop.style.borderRadius).toBe('50%');
        expect(surface.style.getPropertyValue('--fp-aspect')).toBe(String(800 / 600));
    });

    test('should do nothing when the root element is missing', () => {
        expect(() => initialize('missing', helper, 50, 50, 100)).not.toThrow();
    });

    test('should move the window to a clicked point and tell .NET once per frame', () => {
        initialize(rootId, helper, 50, 50, 200);
        loadImage(800, 600);

        pointer('pointerdown', 0.31, 0.5);
        pointer('pointerup', 0.31, 0.5);
        flush();

        expect(helper.invokeMethodAsync).toHaveBeenCalledTimes(1);
        const [name, x, y, zoom] = helper.invokeMethodAsync.mock.calls[0];
        expect(name).toBe('NotifyChanged');
        expect(zoom).toBe(200);
        expect(y).toBe(50);
        const { cx } = centerOf(x, y, cropSize(4 / 3, 1, 200));
        expect(cx).toBeCloseTo(0.31, 1);
    });

    test('should keep the grab offset when dragging from inside the window', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.6, 0.5); // inside the circle, 0.1 right of its center
        pointer('pointermove', 0.7, 0.5);
        pointer('pointerup', 0.7, 0.5);
        flush();

        // The window center follows the pointer minus the grab offset: 0.7 - 0.1 = 0.6 of the photo.
        const [x] = lastNotified();
        expect(x).toBe(90);
    });

    test('should coalesce a fast drag into one notification per frame', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.5, 0.5);
        pointer('pointermove', 0.55, 0.5);
        pointer('pointermove', 0.6, 0.5);
        pointer('pointermove', 0.65, 0.5);
        pointer('pointerup', 0.65, 0.5);
        flush();

        expect(helper.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    test('should not tell .NET while a drag is under way, only when it ends', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.5, 0.5);
        pointer('pointermove', 0.6, 0.5);
        flush();
        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();

        pointer('pointerup', 0.6, 0.5);
        flush();
        expect(helper.invokeMethodAsync).toHaveBeenCalledTimes(1);
        expect(lastNotified()[0]).toBe(90);
    });

    test('should not tell .NET when a press and release leave the framing unchanged', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.5, 0.5);
        pointer('pointerup', 0.5, 0.5);
        flush();

        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should keep the preview and zoom label live during a drag', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.5, 0.5);
        pointer('pointermove', 0.6, 0.5);

        expect(document.querySelector('[data-fp-preview]').getAttribute('style')).toBe(imageStyle(90, 50, 100));
    });

    test('should redraw the zoom label on every slider step but tell .NET only on release', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        slider.value = '150';
        slider.dispatchEvent(new Event('input'));
        flush();

        expect(document.querySelector('[data-fp-zoom-text]').textContent).toBe('1.5×');
        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();

        slider.dispatchEvent(new Event('change'));
        flush();
        expect(helper.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    test('should mark the surface while dragging and clear it on pointer up', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointerdown', 0.5, 0.5);
        expect(surface.classList.contains('focal-picker__surface--dragging')).toBe(true);

        pointer('pointerup', 0.5, 0.5);
        expect(surface.classList.contains('focal-picker__surface--dragging')).toBe(false);
    });

    test('should ignore a press that is not the primary button', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        surface.dispatchEvent(new MouseEvent('pointerdown', { clientX: 10, clientY: 10, button: 2, bubbles: true }));
        flush();

        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should show the grab cursor class over the window when not dragging', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        pointer('pointermove', 0.5, 0.5);
        expect(surface.classList.contains('focal-picker__surface--over-crop')).toBe(true);

        pointer('pointermove', 0.02, 0.02);
        expect(surface.classList.contains('focal-picker__surface--over-crop')).toBe(false);
    });

    test('should move 1 percent per arrow key and 5 with shift on an axis that can move', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        key('ArrowRight');
        flush();
        expect(lastNotified()[0]).toBe(51);

        key('ArrowRight', true);
        flush();
        expect(lastNotified()[0]).toBe(56);

        key('ArrowLeft');
        flush();
        expect(lastNotified()[0]).toBe(55);
    });

    test('should not move on an axis with no overflow', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        key('ArrowDown');
        flush();

        expect(lastNotified()[1]).toBe(50);
    });

    test('should stop at 0 and 100', () => {
        initialize(rootId, helper, 99, 50, 100);
        loadImage(800, 600);

        key('ArrowRight', true);
        flush();
        expect(lastNotified()[0]).toBe(100);

        update(rootId, 2, 50, 100);
        key('ArrowLeft', true);
        flush();
        expect(lastNotified()[0]).toBe(0);
    });

    test('should leave other keys alone', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);
        const event = new KeyboardEvent('keydown', { key: 'a', bubbles: true, cancelable: true });

        surface.dispatchEvent(event);
        flush();

        expect(event.defaultPrevented).toBe(false);
        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should keep the window centered when the zoom changes', () => {
        initialize(rootId, helper, 0, 50, 100); // window center at 0.375 of the photo width
        loadImage(800, 600);

        slider.value = '200';
        slider.dispatchEvent(new Event('input'));
        slider.dispatchEvent(new Event('change'));
        flush();

        const [x, y, zoom] = lastNotified();
        expect(zoom).toBe(200);
        expect(y).toBe(50);
        expect(centerOf(x, y, cropSize(4 / 3, 1, 200)).cx).toBeCloseTo(0.375, 1);
    });

    test('should redraw the outline when .NET updates the framing', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        update(rootId, 0, 50, 200);

        expect(crop.style.width).toBe('37.5%');
        expect(slider.value).toBe('200');
    });

    test('should ignore an update for a picker that is not initialized', () => {
        expect(() => update('missing', 1, 2, 100)).not.toThrow();
    });

    test('should round the outline corners for a rectangle', () => {
        render('rectangle', '2');
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        expect(crop.style.borderRadius).toBe('4px');
    });

    test('should stop reacting after dispose', () => {
        initialize(rootId, helper, 50, 50, 100);
        loadImage(800, 600);

        dispose(rootId);
        key('ArrowRight');
        flush();

        expect(helper.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should ignore a dispose for a picker that is not initialized', () => {
        expect(() => dispose('missing')).not.toThrow();
    });
});
