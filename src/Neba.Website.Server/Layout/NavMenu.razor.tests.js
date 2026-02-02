// NavMenu.razor.tests.js - Unit tests for NavMenu navigation interactivity

import { initialize, dispose, preventBodyScroll } from './NavMenu.razor.js';

// Helper to mock CSS custom properties
function mockBreakpoint(value) {
    jest.spyOn(globalThis, 'getComputedStyle').mockReturnValue({
        getPropertyValue: jest.fn().mockReturnValue(String(value))
    });
}

describe('NavMenu', () => {
    let mockDotNetRef;
    let navbar;
    let menu;
    let toggle;

    // Helper to set up DOM structure
    function setupDOM({ menuActive = false, withDropdowns = false } = {}) {
        document.body.innerHTML = `
            <nav class="neba-navbar">
                <button data-menu-toggle aria-expanded="${!menuActive}">Toggle</button>
                <ul data-menu class="neba-nav-menu ${menuActive ? 'active' : ''}">
                    <li><a href="/">Home</a></li>
                    ${withDropdowns ? `
                    <li class="neba-nav-item" data-action="toggle-dropdown">
                        <a href="#" class="neba-nav-link" aria-haspopup="true" aria-expanded="false">Dropdown</a>
                        <div class="neba-dropdown" role="menu">
                            <a href="/item1" class="neba-dropdown-link" role="menuitem">Item 1</a>
                            <a href="/item2" class="neba-dropdown-link" role="menuitem">Item 2</a>
                        </div>
                    </li>
                    <li class="neba-nav-item" data-action="toggle-dropdown">
                        <a href="#" class="neba-nav-link" aria-haspopup="true" aria-expanded="false">Dropdown 2</a>
                        <div class="neba-dropdown" role="menu">
                            <a href="/item3" class="neba-dropdown-link" role="menuitem">Item 3</a>
                        </div>
                    </li>
                    ` : ''}
                </ul>
            </nav>
        `;
        navbar = document.querySelector('.neba-navbar');
        menu = document.querySelector('[data-menu]');
        toggle = document.querySelector('[data-menu-toggle]');
    }

    beforeEach(() => {
        // Reset DOM
        document.body.innerHTML = '';

        // Create mock DotNet reference
        mockDotNetRef = {
            invokeMethodAsync: jest.fn().mockResolvedValue(undefined)
        };

        // Default breakpoint
        mockBreakpoint(1024);

        // Default window size (mobile)
        Object.defineProperty(globalThis, 'innerWidth', {
            writable: true,
            configurable: true,
            value: 768
        });

        // Default scroll position
        Object.defineProperty(globalThis, 'scrollY', {
            writable: true,
            configurable: true,
            value: 0
        });
    });

    afterEach(() => {
        dispose();
        jest.restoreAllMocks();
    });

    describe('initialize', () => {
        test('should add scroll event listener', () => {
            setupDOM();
            const addEventListenerSpy = jest.spyOn(globalThis, 'addEventListener');

            initialize(mockDotNetRef);

            expect(addEventListenerSpy).toHaveBeenCalledWith(
                'scroll',
                expect.any(Function),
                { passive: true }
            );
        });

        test('should add click event listener to document', () => {
            setupDOM();
            const addEventListenerSpy = jest.spyOn(document, 'addEventListener');

            initialize(mockDotNetRef);

            expect(addEventListenerSpy).toHaveBeenCalledWith(
                'click',
                expect.any(Function)
            );
        });

        test('should add keydown event listener to document', () => {
            setupDOM();
            const addEventListenerSpy = jest.spyOn(document, 'addEventListener');

            initialize(mockDotNetRef);

            expect(addEventListenerSpy).toHaveBeenCalledWith(
                'keydown',
                expect.any(Function)
            );
        });

        test('should perform initial scroll check', () => {
            setupDOM();
            globalThis.scrollY = 50;

            initialize(mockDotNetRef);

            expect(navbar.classList.contains('scrolled')).toBe(true);
        });

        test('should dispose previous initialization if called twice', () => {
            setupDOM();
            const removeEventListenerSpy = jest.spyOn(globalThis, 'removeEventListener');

            initialize(mockDotNetRef);
            initialize(mockDotNetRef);

            expect(removeEventListenerSpy).toHaveBeenCalledWith(
                'scroll',
                expect.any(Function)
            );
        });
    });

    describe('dispose', () => {
        test('should remove scroll event listener', () => {
            setupDOM();
            const removeEventListenerSpy = jest.spyOn(globalThis, 'removeEventListener');

            initialize(mockDotNetRef);
            dispose();

            expect(removeEventListenerSpy).toHaveBeenCalledWith(
                'scroll',
                expect.any(Function)
            );
        });

        test('should remove click event listener', () => {
            setupDOM();
            const removeEventListenerSpy = jest.spyOn(document, 'removeEventListener');

            initialize(mockDotNetRef);
            dispose();

            expect(removeEventListenerSpy).toHaveBeenCalledWith(
                'click',
                expect.any(Function)
            );
        });

        test('should remove keydown event listener', () => {
            setupDOM();
            const removeEventListenerSpy = jest.spyOn(document, 'removeEventListener');

            initialize(mockDotNetRef);
            dispose();

            expect(removeEventListenerSpy).toHaveBeenCalledWith(
                'keydown',
                expect.any(Function)
            );
        });

        test('should be safe to call multiple times', () => {
            setupDOM();
            initialize(mockDotNetRef);

            expect(() => {
                dispose();
                dispose();
            }).not.toThrow();
        });

        test('should be safe to call without initialize', () => {
            expect(() => dispose()).not.toThrow();
        });
    });

    describe('scroll behavior', () => {
        test('should add scrolled class when scrollY > 10', () => {
            setupDOM();
            initialize(mockDotNetRef);

            globalThis.scrollY = 15;
            globalThis.dispatchEvent(new Event('scroll'));

            expect(navbar.classList.contains('scrolled')).toBe(true);
        });

        test('should remove scrolled class when scrollY <= 10', () => {
            setupDOM();
            navbar.classList.add('scrolled');
            initialize(mockDotNetRef);

            globalThis.scrollY = 5;
            globalThis.dispatchEvent(new Event('scroll'));

            expect(navbar.classList.contains('scrolled')).toBe(false);
        });

        test('should add scrolled class at exactly scrollY = 11', () => {
            setupDOM();
            initialize(mockDotNetRef);

            globalThis.scrollY = 11;
            globalThis.dispatchEvent(new Event('scroll'));

            expect(navbar.classList.contains('scrolled')).toBe(true);
        });

        test('should not add scrolled class at exactly scrollY = 10', () => {
            setupDOM();
            initialize(mockDotNetRef);

            globalThis.scrollY = 10;
            globalThis.dispatchEvent(new Event('scroll'));

            expect(navbar.classList.contains('scrolled')).toBe(false);
        });

        test('should handle missing navbar gracefully', () => {
            document.body.innerHTML = '';
            initialize(mockDotNetRef);

            expect(() => {
                globalThis.scrollY = 50;
                globalThis.dispatchEvent(new Event('scroll'));
            }).not.toThrow();
        });
    });

    describe('click outside behavior', () => {
        test('should call CloseMenu when clicking outside open menu on mobile', () => {
            setupDOM({ menuActive: true });
            globalThis.innerWidth = 768; // Mobile width
            initialize(mockDotNetRef);

            // Click on document body (outside menu)
            document.body.click();

            expect(mockDotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CloseMenu');
        });

        test('should not call CloseMenu when clicking inside menu', () => {
            setupDOM({ menuActive: true });
            globalThis.innerWidth = 768;
            initialize(mockDotNetRef);

            menu.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should not call CloseMenu when clicking toggle button', () => {
            setupDOM({ menuActive: true });
            globalThis.innerWidth = 768;
            initialize(mockDotNetRef);

            toggle.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should not call CloseMenu when menu is closed', () => {
            setupDOM({ menuActive: false });
            globalThis.innerWidth = 768;
            initialize(mockDotNetRef);

            document.body.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should not call CloseMenu on desktop width', () => {
            setupDOM({ menuActive: true });
            globalThis.innerWidth = 1200; // Desktop width (> 1024 breakpoint)
            initialize(mockDotNetRef);

            document.body.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should respect custom breakpoint from CSS variable', () => {
            setupDOM({ menuActive: true });
            mockBreakpoint(900); // Custom breakpoint
            globalThis.innerWidth = 950; // Above custom breakpoint
            initialize(mockDotNetRef);

            document.body.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });
    });

    describe('keyboard behavior', () => {
        test('should call CloseMenu when Escape pressed with open menu', () => {
            setupDOM({ menuActive: true });
            initialize(mockDotNetRef);

            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

            expect(mockDotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CloseMenu');
        });

        test('should focus toggle button after Escape closes menu', () => {
            setupDOM({ menuActive: true });
            initialize(mockDotNetRef);
            const focusSpy = jest.spyOn(toggle, 'focus');

            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

            expect(focusSpy).toHaveBeenCalled();
        });

        test('should not call CloseMenu when Escape pressed with closed menu', () => {
            setupDOM({ menuActive: false });
            initialize(mockDotNetRef);

            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should not respond to other keys', () => {
            setupDOM({ menuActive: true });
            initialize(mockDotNetRef);

            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab' }));
            document.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });
    });

    describe('getBreakpoint', () => {
        test('should use default 1024 when CSS variable is empty', () => {
            setupDOM({ menuActive: true });
            jest.spyOn(globalThis, 'getComputedStyle').mockReturnValue({
                getPropertyValue: jest.fn().mockReturnValue('')
            });
            globalThis.innerWidth = 1100; // Above default 1024
            initialize(mockDotNetRef);

            document.body.click();

            // Should not close because width > default breakpoint
            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should use default 1024 when CSS variable returns NaN', () => {
            setupDOM({ menuActive: true });
            jest.spyOn(globalThis, 'getComputedStyle').mockReturnValue({
                getPropertyValue: jest.fn().mockReturnValue('invalid')
            });
            globalThis.innerWidth = 1100;
            initialize(mockDotNetRef);

            document.body.click();

            expect(mockDotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });
    });

    describe('dropdown behavior', () => {
        test('should open dropdown when clicking toggle link', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItem = document.querySelector('[data-action="toggle-dropdown"]');
            const link = dropdownItem.querySelector('.neba-nav-link');

            link.click();

            expect(dropdownItem.classList.contains('active')).toBe(true);
            expect(link.getAttribute('aria-expanded')).toBe('true');
        });

        test('should close dropdown when clicking toggle link again', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItem = document.querySelector('[data-action="toggle-dropdown"]');
            const link = dropdownItem.querySelector('.neba-nav-link');

            // Open
            link.click();
            expect(dropdownItem.classList.contains('active')).toBe(true);

            // Close
            link.click();
            expect(dropdownItem.classList.contains('active')).toBe(false);
            expect(link.getAttribute('aria-expanded')).toBe('false');
        });

        test('should close other dropdowns when opening a new one', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItems = document.querySelectorAll('[data-action="toggle-dropdown"]');
            const firstLink = dropdownItems[0].querySelector('.neba-nav-link');
            const secondLink = dropdownItems[1].querySelector('.neba-nav-link');

            // Open first dropdown
            firstLink.click();
            expect(dropdownItems[0].classList.contains('active')).toBe(true);

            // Open second dropdown - first should close
            secondLink.click();
            expect(dropdownItems[0].classList.contains('active')).toBe(false);
            expect(dropdownItems[1].classList.contains('active')).toBe(true);
        });

        test('should close all dropdowns when clicking outside', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItem = document.querySelector('[data-action="toggle-dropdown"]');
            const link = dropdownItem.querySelector('.neba-nav-link');

            // Open dropdown
            link.click();
            expect(dropdownItem.classList.contains('active')).toBe(true);

            // Click outside
            document.body.click();
            expect(dropdownItem.classList.contains('active')).toBe(false);
        });

        test('should close all dropdowns when Escape is pressed', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItem = document.querySelector('[data-action="toggle-dropdown"]');
            const link = dropdownItem.querySelector('.neba-nav-link');

            // Open dropdown
            link.click();
            expect(dropdownItem.classList.contains('active')).toBe(true);

            // Press Escape
            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
            expect(dropdownItem.classList.contains('active')).toBe(false);
            expect(link.getAttribute('aria-expanded')).toBe('false');
        });

        test('should not close dropdown when clicking inside dropdown menu', () => {
            setupDOM({ withDropdowns: true });
            initialize(mockDotNetRef);

            const dropdownItem = document.querySelector('[data-action="toggle-dropdown"]');
            const link = dropdownItem.querySelector('.neba-nav-link');
            const dropdownLink = dropdownItem.querySelector('.neba-dropdown-link');

            // Open dropdown
            link.click();
            expect(dropdownItem.classList.contains('active')).toBe(true);

            // Click on dropdown item (should navigate, not close)
            dropdownLink.click();

            // Dropdown should still be open (actual navigation would close it)
            expect(dropdownItem.classList.contains('active')).toBe(true);
        });
    });

    describe('preventBodyScroll', () => {
        test('should fix body position when preventing scroll', () => {
            setupDOM();
            globalThis.scrollY = 100;

            preventBodyScroll(true);

            expect(document.body.style.position).toBe('fixed');
            expect(document.body.style.top).toBe('-100px');
            expect(document.body.style.width).toBe('100%');
            expect(document.body.style.overflow).toBe('hidden');
            expect(document.body.dataset.scrollY).toBe('100');
        });

        test('should restore body position when allowing scroll', () => {
            setupDOM();
            const scrollToSpy = jest.spyOn(globalThis, 'scrollTo').mockImplementation(() => {});

            // First prevent scroll
            globalThis.scrollY = 100;
            preventBodyScroll(true);

            // Then restore
            preventBodyScroll(false);

            expect(document.body.style.position).toBe('');
            expect(document.body.style.top).toBe('');
            expect(document.body.style.width).toBe('');
            expect(document.body.style.overflow).toBe('');
            expect(scrollToSpy).toHaveBeenCalledWith(0, 100);
        });

        test('should handle restore without previous scroll position', () => {
            setupDOM();
            const scrollToSpy = jest.spyOn(globalThis, 'scrollTo').mockImplementation(() => {});

            preventBodyScroll(false);

            expect(scrollToSpy).toHaveBeenCalledWith(0, 0);
        });
    });

    describe('edge cases', () => {
        test('should handle null dotNetReference gracefully', () => {
            setupDOM({ menuActive: true });
            initialize(null);

            expect(() => {
                document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
            }).not.toThrow();
        });

        test('should handle missing menu element', () => {
            document.body.innerHTML = '<nav class="neba-navbar"></nav>';
            initialize(mockDotNetRef);

            expect(() => {
                document.body.click();
            }).not.toThrow();
        });

        test('should handle missing toggle element', () => {
            document.body.innerHTML = `
                <nav class="neba-navbar">
                    <ul data-menu class="neba-nav-menu active"></ul>
                </nav>
            `;
            initialize(mockDotNetRef);

            expect(() => {
                document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
            }).not.toThrow();
        });
    });
});
