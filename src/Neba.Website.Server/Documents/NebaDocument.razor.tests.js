import { describe, test, expect, beforeEach, jest } from '@jest/globals';
import { initialize, scrollToHash, openSlideover, closeSlideover, initializeSlideoverContent, dispose } from './NebaDocument.razor.js';

describe('NebaDocument', () => {
  let mockDotNetReference;

  beforeEach(() => {
    jest.clearAllMocks();
    document.body.innerHTML = '';

    // Mock dotNetReference
    mockDotNetReference = {
      invokeMethodAsync: jest.fn()
    };

    // Mock console methods
    globalThis.console.log = jest.fn();
    globalThis.console.warn = jest.fn();
    globalThis.console.error = jest.fn();

    // Mock requestAnimationFrame
    globalThis.requestAnimationFrame = jest.fn((cb) => {
      cb();
      return 1;
    });

    // Mock location
    delete globalThis.location;
    globalThis.location = {
      href: 'http://localhost',
      origin: 'http://localhost',
      hash: ''
    };
  });

  describe('initialize', () => {
    test('should initialize with headings from content', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h2 id="heading2">Heading 2</h2>
          <h1 id="heading3">Another Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close',
        headingLevels: 'h1, h2'
      };

      // Act
      const result = initialize(mockDotNetReference, config);

      // Assert
      expect(result).toBe(true);
      const tocList = document.getElementById('toc-list');
      expect(tocList.innerHTML).toContain('Heading 1');
      expect(tocList.innerHTML).toContain('Heading 2');
      expect(tocList.innerHTML).toContain('Another Heading 1');
    });

    test('should return false when content element not found', () => {
      // Arrange
      document.body.innerHTML = `
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'nonexistent',
        tocListId: 'toc-list'
      };

      // Act
      const result = initialize(mockDotNetReference, config);

      // Assert
      expect(result).toBe(false);
      expect(globalThis.console.error).toHaveBeenCalledWith(
        expect.stringContaining('Content element not found'),
        'nonexistent'
      );
    });

    test('should initialize successfully even without headings (for link navigation)', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <p>No headings here</p>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      // Act
      const result = initialize(mockDotNetReference, config);

      // Assert
      // Returns true because link navigation is still set up even without headings
      expect(result).toBe(true);
    });

    test('should escape HTML in heading text to prevent XSS', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1"><script>alert('xss')</script>Test Heading</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      // Act
      initialize(mockDotNetReference, config);

      // Assert
      const tocList = document.getElementById('toc-list');
      expect(tocList.innerHTML).not.toContain('<script>');
    });

    test('should auto-generate IDs for headings without IDs', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>No ID Heading 1</h1>
          <h2>No ID Heading 2</h2>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      // Act
      initialize(mockDotNetReference, config);

      // Assert
      const headings = document.querySelectorAll('#content h1, #content h2');
      expect(headings[0].id).toBe('heading-0');
      expect(headings[1].id).toBe('heading-1');
    });

    test('should set up mobile modal open/close handlers', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      // Act
      initialize(mockDotNetReference, config);

      const mobileButton = document.getElementById('toc-mobile-button');
      const modal = document.getElementById('toc-modal');

      // Simulate click on mobile button
      mobileButton.click();

      // Assert
      expect(modal.classList.contains('active')).toBe(true);
      expect(document.body.style.overflow).toBe('hidden');
    });

    test('should close modal on overlay click', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal" class="active"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      document.body.style.overflow = 'hidden';

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      // Act
      initialize(mockDotNetReference, config);

      const overlay = document.getElementById('toc-modal-overlay');
      const modal = document.getElementById('toc-modal');

      overlay.click();

      // Assert
      expect(modal.classList.contains('active')).toBe(false);
      expect(document.body.style.overflow).toBe('');
    });

    test('should close modal on Escape key', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal" class="active"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      // Act
      initialize(mockDotNetReference, config);

      const modal = document.getElementById('toc-modal');
      const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
      document.dispatchEvent(escapeEvent);

      // Assert
      expect(modal.classList.contains('active')).toBe(false);
    });

    test('should populate both desktop and mobile TOC lists', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
          <h2>Heading 2</h2>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list'
      };

      // Act
      initialize(mockDotNetReference, config);

      // Assert
      const tocList = document.getElementById('toc-list');
      const mobileTocList = document.getElementById('toc-mobile-list');

      expect(tocList.innerHTML).toContain('Heading 1');
      expect(tocList.innerHTML).toContain('Heading 2');
      expect(mobileTocList.innerHTML).toContain('Heading 1');
      expect(mobileTocList.innerHTML).toContain('Heading 2');
    });

    test('should handle Ctrl+Click to allow opening in new tab', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <a href="https://example.com">External Link</a>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      initialize(mockDotNetReference, config);

      // Act
      const link = document.querySelector('a[href="https://example.com"]');
      const clickEvent = new MouseEvent('click', {
        bubbles: true,
        cancelable: true,
        ctrlKey: true
      });

      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(false);
    });

    test('should invoke Blazor callback for internal links', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="/bylaws">Internal Link</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover',
        slideoverOverlayId: 'slideover-overlay',
        slideoverCloseId: 'slideover-close'
      };

      initialize(mockDotNetReference, config);

      // Act
      const link = document.querySelector('a[href="/bylaws"]');
      link.click();

      // Assert
      expect(mockDotNetReference.invokeMethodAsync).toHaveBeenCalledWith(
        'OnInternalLinkClicked',
        'bylaws'
      );
    });

    test('should handle anchor links within content', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="section1">Section 1</h1>
          <a href="#section1">Link to Section 1</a>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      initialize(mockDotNetReference, config);

      // Act
      const link = content.querySelector('a[href="#section1"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(clickEvent);

      // Assert
      expect(content.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });
  });

  describe('scrollToHash', () => {
    test('should scroll to element when hash exists', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content" style="height: 2000px; overflow: auto;">
          <h1 id="heading1" style="margin-top: 1000px;">Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      globalThis.location.hash = '#heading1';
      globalThis.pageYOffset = 0;

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      // Act
      scrollToHash('content', 'toc-list');

      // Assert
      expect(content.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should return early when no hash in URL', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      globalThis.location.hash = '';

      // Act
      scrollToHash('content', 'toc-list');

      // Assert - function should return early without errors
      expect(globalThis.console.error).not.toHaveBeenCalled();
    });

    test('should update active link in TOC', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list">
          <li><a class="toc-link" data-target="heading1">Heading 1</a></li>
          <li><a class="toc-link active" data-target="heading2">Heading 2</a></li>
        </ul>
      `;

      globalThis.location.hash = '#heading1';

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      // Act
      scrollToHash('content', 'toc-list');

      // Assert
      const activeLink = document.querySelector('.toc-link.active');
      expect(activeLink.dataset.target).toBe('heading1');
    });
  });

  describe('openSlideover', () => {
    test('should open slideover panel', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="slideover"></div>
      `;

      // Act
      openSlideover('slideover');

      // Assert
      const slideover = document.getElementById('slideover');
      expect(slideover.classList.contains('active')).toBe(true);
      expect(document.body.style.overflow).toBe('hidden');
    });

    test('should handle missing slideover element gracefully', () => {
      // Act & Assert - should not throw
      expect(() => openSlideover('nonexistent')).not.toThrow();
    });
  });

  describe('closeSlideover', () => {
    test('should close slideover panel', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="slideover" class="active"></div>
      `;
      document.body.style.overflow = 'hidden';

      // Act
      closeSlideover('slideover');

      // Assert
      const slideover = document.getElementById('slideover');
      expect(slideover.classList.contains('active')).toBe(false);
      expect(document.body.style.overflow).toBe('');
    });

    test('should handle missing slideover element gracefully', () => {
      // Act & Assert - should not throw
      expect(() => closeSlideover('nonexistent')).not.toThrow();
    });
  });

  describe('dispose', () => {
    test('should clean up event listeners', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      initialize(mockDotNetReference, config);

      // Act
      dispose();

      // Assert - should not throw
      expect(() => dispose()).not.toThrow();
    });

    test('should allow reinitialization after dispose', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      // Act
      initialize(mockDotNetReference, config);
      dispose();
      const result = initialize(mockDotNetReference, config);

      // Assert
      expect(result).toBe(true);
    });
  });

  describe('scroll spy functionality', () => {
    test('should update active link when scrolling content', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content" style="height: 500px; overflow: auto;">
          <h1 id="heading1" style="margin-top: 50px;">Heading 1</h1>
          <div style="height: 300px;"></div>
          <h1 id="heading2" style="margin-top: 100px;">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      initialize(mockDotNetReference, config);

      const content = document.getElementById('content');

      // Act
      const scrollEvent = new Event('scroll');
      content.dispatchEvent(scrollEvent);

      // Assert
      expect(globalThis.requestAnimationFrame).toHaveBeenCalled();
    });

    test('should click desktop TOC link to scroll to section', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content" style="height: 500px; overflow: auto;">
          <h1 id="heading1">Heading 1</h1>
          <div style="height: 500px;"></div>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      initialize(mockDotNetReference, config);

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      // Act
      const tocLink = document.querySelector('.toc-link[data-target="heading2"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      tocLink.dispatchEvent(clickEvent);

      // Assert
      expect(content.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should click mobile TOC link to scroll and close modal', () => {
      // Arrange
      jest.useFakeTimers();

      document.body.innerHTML = `
        <div id="content" style="height: 500px; overflow: auto;">
          <h1 id="heading1">Heading 1</h1>
          <div style="height: 500px;"></div>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal" class="active"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      globalThis.scrollTo = jest.fn();
      globalThis.pageYOffset = 0;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      initialize(mockDotNetReference, config);

      const modal = document.getElementById('toc-modal');

      // Act
      const mobileTocLink = document.querySelector('#toc-mobile-list .toc-link[data-target="heading2"]');
      mobileTocLink.click();

      // Assert
      expect(modal.classList.contains('active')).toBe(false);

      jest.advanceTimersByTime(300);

      expect(globalThis.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );

      jest.useRealTimers();
    });
  });

  describe('edge cases', () => {
    test('should handle link with empty href', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="">Empty href</a>
          <a href="#">Hash only</a>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      initialize(mockDotNetReference, config);

      // Act & Assert
      const emptyLink = document.querySelector('a[href=""]');
      const hashLink = document.querySelector('a[href="#"]');

      expect(() => emptyLink.click()).not.toThrow();
      expect(() => hashLink.click()).not.toThrow();
    });

    test('should handle Cmd+Click (metaKey) to open in new tab', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="/bylaws">Internal Link</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      };

      initialize(mockDotNetReference, config);

      // Act
      const link = document.querySelector('a[href="/bylaws"]');
      const clickEvent = new MouseEvent('click', {
        bubbles: true,
        cancelable: true,
        metaKey: true
      });

      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(false);
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should handle missing TOC list but with headings', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading With ID</h1>
          <h2 id="heading2">Another Heading</h2>
        </div>
      `;

      const config = {
        contentId: 'content',
        tocListId: null
      };

      // Act
      const result = initialize(mockDotNetReference, config);

      // Assert - should initialize successfully even without TOC list
      expect(result).toBe(true);
    });

    test('should not close modal when modal is not active on Escape', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
        <div id="toc-modal"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      initialize(mockDotNetReference, config);

      const modal = document.getElementById('toc-modal');

      // Act
      const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
      document.dispatchEvent(escapeEvent);

      // Assert
      expect(modal.classList.contains('active')).toBe(false);
    });

    test('should close slideover on Escape key', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover" class="active"></div>
        <div id="slideover-overlay"></div>
        <button id="slideover-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover',
        slideoverOverlayId: 'slideover-overlay',
        slideoverCloseId: 'slideover-close'
      };

      initialize(mockDotNetReference, config);

      const slideover = document.getElementById('slideover');

      // Act
      const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
      document.dispatchEvent(escapeEvent);

      // Assert
      expect(slideover.classList.contains('active')).toBe(false);
    });

    test('should handle anchor navigation with non-scrollable content', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="section1">Section 1</h1>
          <a href="#section1">Link to Section 1</a>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      const content = document.getElementById('content');
      Object.defineProperty(content, 'scrollHeight', { value: 100, configurable: true });
      Object.defineProperty(content, 'clientHeight', { value: 100, configurable: true });
      content.scrollTo = null;

      globalThis.scrollTo = jest.fn();
      globalThis.scrollY = 0;

      initialize(mockDotNetReference, config);

      // Act
      const link = content.querySelector('a[href="#section1"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(clickEvent);

      // Assert
      expect(globalThis.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should navigate to anchor when clicking hash link', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1 id="section1">Section 1</h1>
          <a href="#section1">Link to Section 1</a>
        </div>
        <ul id="toc-list"></ul>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list'
      };

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      initialize(mockDotNetReference, config);

      // Act
      const link = content.querySelector('a[href="#section1"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(true); // Event should be prevented
      expect(content.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should allow external protocol links like mailto and tel', () => {
      // Arrange
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="mailto:test@example.com">Email</a>
          <a href="tel:+1234567890">Phone</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      };

      initialize(mockDotNetReference, config);

      // Act/Assert
      const emailLink = document.querySelector('a[href^="mailto:"]');
      const phoneLink = document.querySelector('a[href^="tel:"]');

      expect(() => emailLink.click()).not.toThrow();
      expect(() => phoneLink.click()).not.toThrow();
    });
  });

  describe('initializeSlideoverContent', () => {
    beforeEach(() => {
      // Initialize main component first to set up dotNetReference
      document.body.innerHTML = `
        <div id="main-content">
          <h1>Main Content</h1>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover">
          <div id="slideover-content"></div>
        </div>
      `;

      const config = {
        contentId: 'main-content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      };

      initialize(mockDotNetReference, config);
    });

    test('should handle hash links within slideover content', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1 id="section1">Section 1</h1>
        <a href="#section1">Link to Section 1</a>
      `;

      slideoverContent.scrollTo = jest.fn();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="#section1"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(true);
      expect(slideoverContent.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should invoke callback for internal document links in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Tournament Rules</h1>
        <a href="/about/bylaws">See Bylaws</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="/about/bylaws"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(clickEvent);

      // Assert
      expect(mockDotNetReference.invokeMethodAsync).toHaveBeenCalledWith(
        'OnInternalLinkClicked',
        'about/bylaws'
      );
    });

    test('should handle Google Docs anchor format in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1 id="article-1-name" data-original-id="h.abc123">Article 1 - Name</h1>
        <a href="#heading=h.abc123">Link to Article 1</a>
      `;

      slideoverContent.scrollTo = jest.fn();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="#heading=h.abc123"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(clickEvent);

      // Assert
      expect(slideoverContent.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should handle same-page links with hash in slideover', () => {
      // Arrange
      globalThis.location.pathname = '/bylaws';

      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1 id="section1">Section 1</h1>
        <a href="/bylaws#section1">Link to Section 1</a>
      `;

      slideoverContent.scrollTo = jest.fn();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="/bylaws#section1"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(true);
      expect(slideoverContent.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({
          behavior: 'smooth'
        })
      );
    });

    test('should ignore external links in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Content</h1>
        <a href="https://external.com">External Link</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="https://external.com"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(false);
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should handle Ctrl+Click in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Content</h1>
        <a href="/bylaws">Bylaws Link</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="/bylaws"]');
      const clickEvent = new MouseEvent('click', {
        bubbles: true,
        cancelable: true,
        ctrlKey: true
      });
      const defaultPrevented = !link.dispatchEvent(clickEvent);

      // Assert
      expect(defaultPrevented).toBe(false);
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should handle empty href in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Content</h1>
        <a href="">Empty</a>
        <a href="#">Hash only</a>
      `;

      // Act
      initializeSlideoverContent('slideover-content');

      const emptyLink = slideoverContent.querySelector('a[href=""]');
      const hashLink = slideoverContent.querySelector('a[href="#"]');

      // Assert
      expect(() => emptyLink.click()).not.toThrow();
      expect(() => hashLink.click()).not.toThrow();
    });

    test('should handle missing slideover content element gracefully', () => {
      // Act & Assert
      expect(() => initializeSlideoverContent('nonexistent')).not.toThrow();
    });

    test('should allow mailto and tel links in slideover', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Contact</h1>
        <a href="mailto:test@example.com">Email</a>
        <a href="tel:+1234567890">Phone</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act
      initializeSlideoverContent('slideover-content');

      const emailLink = slideoverContent.querySelector('a[href^="mailto:"]');
      const phoneLink = slideoverContent.querySelector('a[href^="tel:"]');

      emailLink.click();
      phoneLink.click();

      // Assert
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should remove existing event listeners before adding new ones', () => {
      // Arrange
      const slideoverContent = document.getElementById('slideover-content');
      slideoverContent.innerHTML = `
        <h1>Content</h1>
        <a href="/bylaws">Bylaws</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act - Initialize twice
      initializeSlideoverContent('slideover-content');
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="/bylaws"]');
      link.click();

      // Assert - Should only be called once (not twice if handlers were duplicated)
      expect(mockDotNetReference.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });
  });
});
