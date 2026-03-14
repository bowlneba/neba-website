import { describe, test, expect, beforeEach, jest } from '@jest/globals';
import { setBodyOverflow, focusElement, enableFocusTrap, disableFocusTrap } from './NebaModal.razor.js';

/**
 * Makes an element appear "visible" to isFocusable by overriding getClientRects
 * to return a non-empty list (jsdom always returns empty by default).
 */
function makeVisible(el) {
    el.getClientRects = () => [{}]; // .length === 1
}

describe('NebaModal', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        document.body.style.overflow = '';
        disableFocusTrap(); // reset module-level trap state
    });

    describe('setBodyOverflow', () => {
        test('should set body overflow to hidden when hidden is true', () => {
            setBodyOverflow(true);

            expect(document.body.style.overflow).toBe('hidden');
        });

        test('should clear body overflow when hidden is false', () => {
            document.body.style.overflow = 'hidden';

            setBodyOverflow(false);

            expect(document.body.style.overflow).toBe('');
        });
    });

    describe('focusElement', () => {
        test('should call focus with preventScroll option on a valid element', () => {
            const el = document.createElement('button');
            document.body.appendChild(el);
            const focusSpy = jest.spyOn(el, 'focus');

            focusElement(el);

            expect(focusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should return early without throwing when element is null', () => {
            expect(() => focusElement(null)).not.toThrow();
        });

        test('should return early without throwing when focus is not a function', () => {
            expect(() => focusElement({ focus: 'not-a-function' })).not.toThrow();
        });
    });

    describe('enableFocusTrap', () => {
        test('should return early without throwing when element is null', () => {
            expect(() => enableFocusTrap(null)).not.toThrow();
        });

        test('should not intercept non-Tab keys', () => {
            document.body.innerHTML = '<div id="dialog"><button id="btn">Btn</button></div>';
            const dialog = document.getElementById('dialog');
            const btn = document.getElementById('btn');
            makeVisible(btn);
            const focusSpy = jest.spyOn(btn, 'focus');

            enableFocusTrap(dialog);

            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true }));

            expect(focusSpy).not.toHaveBeenCalled();
        });

        test('should prevent default and focus container when no focusable elements on Tab', () => {
            document.body.innerHTML = '<div id="dialog"></div>';
            const dialog = document.getElementById('dialog');
            const focusSpy = jest.spyOn(dialog, 'focus');

            enableFocusTrap(dialog);

            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(focusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should wrap Tab forward from last focusable to first', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="first">First</button>
                    <button id="last">Last</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const first = document.getElementById('first');
            const last = document.getElementById('last');
            makeVisible(first);
            makeVisible(last);

            enableFocusTrap(dialog);
            last.focus(); // set document.activeElement to last

            const firstFocusSpy = jest.spyOn(first, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(firstFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should wrap Shift+Tab backward from first focusable to last', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="first">First</button>
                    <button id="last">Last</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const first = document.getElementById('first');
            const last = document.getElementById('last');
            makeVisible(first);
            makeVisible(last);

            enableFocusTrap(dialog);
            first.focus(); // set document.activeElement to first

            const lastFocusSpy = jest.spyOn(last, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true }));

            expect(lastFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should redirect Tab to first focusable when focus is outside dialog', () => {
            document.body.innerHTML = `
                <button id="outside">Outside</button>
                <div id="dialog">
                    <button id="first">First</button>
                    <button id="last">Last</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const outside = document.getElementById('outside');
            const first = document.getElementById('first');
            const last = document.getElementById('last');
            makeVisible(first);
            makeVisible(last);

            enableFocusTrap(dialog);
            outside.focus(); // focus is outside the dialog

            const firstFocusSpy = jest.spyOn(first, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(firstFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should redirect Shift+Tab to last focusable when focus is outside dialog', () => {
            document.body.innerHTML = `
                <button id="outside">Outside</button>
                <div id="dialog">
                    <button id="first">First</button>
                    <button id="last">Last</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const outside = document.getElementById('outside');
            const first = document.getElementById('first');
            const last = document.getElementById('last');
            makeVisible(first);
            makeVisible(last);

            enableFocusTrap(dialog);
            outside.focus();

            const lastFocusSpy = jest.spyOn(last, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true }));

            expect(lastFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should disable previous trap when enabling a new one', () => {
            document.body.innerHTML = `
                <div id="d1"><button id="b1">B1</button></div>
                <div id="d2"><button id="b2">B2</button></div>
            `;
            const d1 = document.getElementById('d1');
            const d2 = document.getElementById('d2');
            const b1 = document.getElementById('b1');
            const b2 = document.getElementById('b2');
            makeVisible(b1);
            makeVisible(b2);

            enableFocusTrap(d1);
            enableFocusTrap(d2); // should remove d1's listener

            // d1's trap was removed — Tab on d1 should not wrap b1
            b1.focus();
            const b1FocusSpy = jest.spyOn(b1, 'focus');
            d1.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(b1FocusSpy).not.toHaveBeenCalled();
        });

        test('should exclude elements with aria-hidden="true" from focusable set', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="visible">Visible</button>
                    <div id="aria-div" tabindex="0" aria-hidden="true">Hidden</div>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const visible = document.getElementById('visible');
            const ariaDiv = document.getElementById('aria-div');
            makeVisible(visible);
            makeVisible(ariaDiv); // visible in layout but aria-hidden

            enableFocusTrap(dialog);
            visible.focus();

            // Only visible is focusable — Tab from last wraps to first (both = visible)
            const visibleFocusSpy = jest.spyOn(visible, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(visibleFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should exclude elements with disabled attribute from focusable set', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="btn">Enabled</button>
                    <div id="disabled-div" tabindex="0">Disabled via attr</div>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const btn = document.getElementById('btn');
            const disabledDiv = document.getElementById('disabled-div');
            makeVisible(btn);
            makeVisible(disabledDiv);
            disabledDiv.setAttribute('disabled', ''); // disabled but has tabindex — excluded by isFocusable

            enableFocusTrap(dialog);
            btn.focus();

            // Only btn is focusable — Tab from last (btn) wraps to first (btn)
            const btnFocusSpy = jest.spyOn(btn, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(btnFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });

        test('should exclude elements with no layout rects from focusable set', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="visible">Visible</button>
                    <button id="invisible">Invisible</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const visible = document.getElementById('visible');
            // 'invisible' is not makeVisible — jsdom returns empty getClientRects by default

            makeVisible(visible);

            enableFocusTrap(dialog);
            visible.focus();

            // Only visible is focusable — Tab wraps to itself
            const visibleFocusSpy = jest.spyOn(visible, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(visibleFocusSpy).toHaveBeenCalledWith({ preventScroll: true });
        });
    });

    describe('disableFocusTrap', () => {
        test('should remove keydown handler so Tab no longer wraps', () => {
            document.body.innerHTML = `
                <div id="dialog">
                    <button id="first">First</button>
                    <button id="last">Last</button>
                </div>
            `;
            const dialog = document.getElementById('dialog');
            const first = document.getElementById('first');
            const last = document.getElementById('last');
            makeVisible(first);
            makeVisible(last);

            enableFocusTrap(dialog);
            disableFocusTrap();

            last.focus();
            const firstFocusSpy = jest.spyOn(first, 'focus');
            dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));

            expect(firstFocusSpy).not.toHaveBeenCalled();
        });

        test('should be safe to call without an active trap', () => {
            expect(() => disableFocusTrap()).not.toThrow();
        });

        test('should be safe to call multiple times', () => {
            expect(() => {
                disableFocusTrap();
                disableFocusTrap();
            }).not.toThrow();
        });
    });
});
