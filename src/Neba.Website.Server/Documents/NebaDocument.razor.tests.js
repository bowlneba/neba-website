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

    // Keep a stable jsdom URL without reassigning window.location.
    globalThis.history.replaceState(null, '', 'http://localhost/');
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

    test('should not populate TOC when headings list is empty', () => {
      document.body.innerHTML = `
        <div id="content"><p>No headings here</p></div>
        <ul id="toc-list"></ul>
      `;

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      expect(document.getElementById('toc-list').innerHTML).toBe('');
    });

    test('should apply toc-item-h1 class to h1 and toc-item-h2 to h2', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="title">Title</h1>
          <h2 id="subtitle">Subtitle</h2>
        </div>
        <ul id="toc-list"></ul>
      `;

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      const tocList = document.getElementById('toc-list');
      expect(tocList.querySelector('.toc-item-h1')).not.toBeNull();
      expect(tocList.querySelector('.toc-item-h2')).not.toBeNull();
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

    test('should not close modal on non-Escape key', () => {
      document.body.innerHTML = `
        <div id="content"><h1>Heading 1</h1></div>
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

      initialize(mockDotNetReference, config);
      const modal = document.getElementById('toc-modal');

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

      expect(modal.classList.contains('active')).toBe(true);
    });

    test('should not close modal when Escape pressed but modal is not active', () => {
      document.body.innerHTML = `
        <div id="content"><h1>Heading 1</h1></div>
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

      initialize(mockDotNetReference, config);
      document.body.style.overflow = 'hidden'; // pre-condition

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

      // overflow should NOT be reset because modal was not active
      expect(document.body.style.overflow).toBe('hidden');
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

    test('should generate TOC HTML with correct ul and li class structure', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="h1">Title</h1>
          <h2 id="h2">Subtitle</h2>
        </div>
        <ul id="toc-list"></ul>
      `;

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      const tocList = document.getElementById('toc-list');
      expect(tocList.querySelector('ul.toc-list')).not.toBeNull();
      expect(tocList.querySelector('ul.toc-list > li.toc-item-h1')).not.toBeNull();
      expect(tocList.querySelector('ul.toc-list > li.toc-item-h2')).not.toBeNull();
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

      globalThis.history.replaceState(null, '', 'http://localhost/#heading1');
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

      globalThis.history.replaceState(null, '', 'http://localhost/');

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      // Act
      scrollToHash('content', 'toc-list');

      // Assert - function should return early without scrolling
      expect(content.scrollTo).not.toHaveBeenCalled();
    });

    test('should strip heading= prefix and scroll to matching element', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="section1">Section 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      globalThis.history.replaceState(null, '', 'http://localhost/#heading=section1');
      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      scrollToHash('content', 'toc-list');

      expect(content.scrollTo).toHaveBeenCalledWith(expect.objectContaining({ behavior: 'smooth' }));
    });

    test('should not call replaceState when hash already matches target', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      globalThis.history.replaceState(null, '', 'http://localhost/#heading1');
      const content = document.getElementById('content');
      content.scrollTo = jest.fn();
      const replaceStateSpy = jest.spyOn(globalThis.history, 'replaceState');

      scrollToHash('content', 'toc-list');

      expect(replaceStateSpy).not.toHaveBeenCalled();
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

      globalThis.history.replaceState(null, '', 'http://localhost/#heading1');

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

    test('should apply active class to initially visible heading in TOC', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      // updateActiveLink() is called once at end of setupScrollSpy.
      // jsdom getBoundingClientRect returns zeros so distanceFromTop=0 → in active window.
      const activeLink = document.querySelector('.toc-link.active');
      expect(activeLink).not.toBeNull();
      expect(activeLink.dataset.target).toBe('heading1');
    });

    test('should not mark any heading active when all headings are fully scrolled past', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const content = document.getElementById('content');
      const heading1 = document.getElementById('heading1');

      // distanceFromTop = 50 - 100 = -50; height=30 so -headingRect.height = -30
      // -50 >= -30 → FALSE → fails primary window; fallback: -50 >= 0 → also FALSE
      content.getBoundingClientRect = () => ({ top: 100, height: 400, bottom: 500 });
      heading1.getBoundingClientRect = () => ({ top: 50, height: 30, bottom: 80 });

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      // With `if (activeHeading) → if (true)` mutation, null.id throws — kills that mutation.
      expect(document.querySelector('.toc-link.active')).toBeNull();
    });

    test('should select closest heading when multiple are in the active window', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const content = document.getElementById('content');
      content.getBoundingClientRect = () => ({ top: 0, height: 400, bottom: 400 });
      document.getElementById('heading1').getBoundingClientRect = () => ({ top: 20, height: 30, bottom: 50 }); // distanceFromTop=20
      document.getElementById('heading2').getBoundingClientRect = () => ({ top: 60, height: 30, bottom: 90 }); // distanceFromTop=60

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      // |20| < |60| → heading1 wins. With `Math.abs < minDistance → true` mutation,
      // heading2 (last processed) would overwrite → test fails on that mutant.
      const activeLink = document.querySelector('.toc-link.active');
      expect(activeLink).not.toBeNull();
      expect(activeLink.dataset.target).toBe('heading1');
    });

    test('should select partially scrolled-past heading still within its own height', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const content = document.getElementById('content');
      content.getBoundingClientRect = () => ({ top: 60, height: 400, bottom: 460 });
      document.getElementById('heading1').getBoundingClientRect = () => ({ top: 50, height: 30, bottom: 80 });
      // distanceFromTop = 50 - 60 = -10; -headingRect.height = -30; -10 >= -30 → in window
      // With `-headingRect.height → +headingRect.height` mutation: -10 >= +30 → FALSE → no active link

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      const activeLink = document.querySelector('.toc-link.active');
      expect(activeLink).not.toBeNull();
      expect(activeLink.dataset.target).toBe('heading1');
    });

    test('should use fallback to select nearest heading below when none are in primary window', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      const content = document.getElementById('content');
      content.getBoundingClientRect = () => ({ top: 0, height: 400, bottom: 400 });
      document.getElementById('heading1').getBoundingClientRect = () => ({ top: 120, height: 30, bottom: 150 }); // distanceFromTop=120 > 100 → primary fails; fallback: 120 >= 0
      document.getElementById('heading2').getBoundingClientRect = () => ({ top: 200, height: 30, bottom: 230 }); // farther

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      // With `!activeHeading → false` mutation, fallback never runs → null → test fails.
      const activeLink = document.querySelector('.toc-link.active');
      expect(activeLink).not.toBeNull();
      expect(activeLink.dataset.target).toBe('heading1');
    });

    test('should scroll the .toc-sticky container to keep active link visible', () => {
      document.body.innerHTML = `
        <div class="toc-sticky">
          <ul id="toc-list"></ul>
        </div>
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
        </div>
      `;

      const tocContainer = document.querySelector('.toc-sticky');
      tocContainer.scrollTo = jest.fn();
      Object.defineProperty(tocContainer, 'scrollTop', { value: 0, configurable: true });

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      // scrollTocToActiveLink is called from updateActiveLink() at end of setupScrollSpy.
      // With BlockStatement mutation (body → {}), scrollTo is never called.
      expect(tocContainer.scrollTo).toHaveBeenCalledWith(
        expect.objectContaining({ behavior: 'smooth' })
      );
    });

    test('should scroll to exact calculated position when clicking desktop TOC link', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
          <h1 id="heading2">Heading 2</h1>
        </div>
        <ul id="toc-list"></ul>
      `;

      initialize(mockDotNetReference, { contentId: 'content', tocListId: 'toc-list' });

      const content = document.getElementById('content');
      const heading2 = document.getElementById('heading2');

      content.scrollTo = jest.fn();
      Object.defineProperty(content, 'scrollTop', { value: 50, configurable: true });
      content.getBoundingClientRect = () => ({ top: 10, height: 400, bottom: 410 });
      heading2.getBoundingClientRect = () => ({ top: 210, height: 30, bottom: 240 });
      // scrollPosition = 50 + (210 - 10) - 0 = 250
      // With `content.scrollTop - (...)` mutation: 50 - 200 = -150
      // With `targetRect.top + contentRect.top` mutation: 50 + (210 + 10) = 270

      document.querySelector('.toc-link[data-target="heading2"]')
        .dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));

      expect(content.scrollTo).toHaveBeenCalledWith({ top: 250, behavior: 'smooth' });
    });

    test('should scroll to exact calculated position when clicking mobile TOC link', () => {
      jest.useFakeTimers();

      document.body.innerHTML = `
        <div id="content">
          <h1 id="heading1">Heading 1</h1>
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
      globalThis.pageYOffset = 100; // non-zero: distinguishes + from - in arithmetic mutations
      // offsetPosition = getBoundingClientRect().top(0) + pageYOffset(100) - navbarHeight(80) = 20
      // With `+ navbarHeight` mutation: 0 + 100 + 80 = 180
      // With `- pageYOffset` mutation: 0 - 100 - 80 = -180

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

      document.querySelector('#toc-mobile-list .toc-link[data-target="heading2"]').click();
      jest.advanceTimersByTime(300);

      expect(globalThis.scrollTo).toHaveBeenCalledWith({ top: 20, behavior: 'smooth' });

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

    test('should not throw when mobile modal button is absent but other modal elements exist', () => {
      // Kills the first || → && mutation in setupMobileModal's guard:
      // !button || !modal || !overlay || !close
      // Without this test, mutating the first || to && passes because all-or-none
      // tests never exercise the partial-present scenario.
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <div id="toc-modal"></div>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'nonexistent-button', // absent — getElementById returns null
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      expect(() => initialize(mockDotNetReference, config)).not.toThrow();
    });

    test('should not throw when mobile modal is absent but button and overlay exist', () => {
      // Kills the second || → && mutation: !button || (!modal && !overlay) || !close
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal-overlay"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'nonexistent-modal', // absent
        tocModalOverlayId: 'toc-modal-overlay',
        tocModalCloseId: 'toc-modal-close'
      };

      expect(() => initialize(mockDotNetReference, config)).not.toThrow();
    });

    test('should not throw when mobile modal overlay is absent but other elements exist', () => {
      // Kills the third || → && mutation: !button || !modal || (!overlay && !close)
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <ul id="toc-mobile-list"></ul>
        <button id="toc-mobile-button"></button>
        <div id="toc-modal"></div>
        <button id="toc-modal-close"></button>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        tocMobileListId: 'toc-mobile-list',
        tocMobileButtonId: 'toc-mobile-button',
        tocModalId: 'toc-modal',
        tocModalOverlayId: 'nonexistent-overlay', // absent
        tocModalCloseId: 'toc-modal-close'
      };

      expect(() => initialize(mockDotNetReference, config)).not.toThrow();
    });

    test('should not throw when slideover overlay is present but close button is absent', () => {
      // Kills the second && → || mutation in setupInternalLinkNavigation:
      // slideover && slideoverOverlay || slideoverClose
      // With that mutation, slideover+overlay present makes it truthy → enters block
      // → slideoverClose.addEventListener on null → crash.
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
        <div id="slideover-overlay"></div>
      `;

      const config = {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover',
        slideoverOverlayId: 'slideover-overlay',
        slideoverCloseId: 'nonexistent-close' // absent — getElementById returns null
      };

      expect(() => initialize(mockDotNetReference, config)).not.toThrow();
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

    test('should not intercept external links from other origins', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="https://external.com/page">External</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      initialize(mockDotNetReference, {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      });

      const link = document.querySelector('a[href^="https://external.com"]');
      const defaultPrevented = !link.dispatchEvent(
        new MouseEvent('click', { bubbles: true, cancelable: true })
      );

      // With `isInternal → true` mutation, external link opens in slideover
      expect(defaultPrevented).toBe(false);
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should not intercept tel: links', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1>Heading</h1>
          <a href="tel:+15550001234">Phone</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      initialize(mockDotNetReference, {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      });

      const link = document.querySelector('a[href^="tel:"]');
      const defaultPrevented = !link.dispatchEvent(
        new MouseEvent('click', { bubbles: true, cancelable: true })
      );

      expect(defaultPrevented).toBe(false);
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should handle same-page hash link by scrolling rather than Blazor callback', () => {
      document.body.innerHTML = `
        <div id="content">
          <h1 id="section1">Section 1</h1>
          <a href="http://localhost/#section1">Same-page link</a>
        </div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
      `;

      const content = document.getElementById('content');
      content.scrollTo = jest.fn();

      initialize(mockDotNetReference, {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover'
      });

      document.querySelector('a[href="http://localhost/#section1"]').click();

      // linkUrl.pathname === currentPath ('/') && linkUrl.hash → scrolls, no Blazor call.
      // With `pathname === currentPath → true` mutation, any internal link triggers scroll.
      // With `&& linkUrl.hash → || linkUrl.hash` mutation, different-page links also scroll.
      expect(content.scrollTo).toHaveBeenCalled();
      expect(mockDotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('should reset body overflow when closing slideover via close button', () => {
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <div id="slideover" class="active"></div>
        <div id="slideover-overlay"></div>
        <button id="slideover-close"></button>
      `;
      document.body.style.overflow = 'hidden';

      initialize(mockDotNetReference, {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover',
        slideoverOverlayId: 'slideover-overlay',
        slideoverCloseId: 'slideover-close'
      });

      document.getElementById('slideover-close').click();

      // With `'' → "Stryker was here!"` StringLiteral mutation, overflow is not reset to ''
      expect(document.body.style.overflow).toBe('');
    });

    test('should not close slideover on Escape when slideover is not active', () => {
      document.body.innerHTML = `
        <div id="content"><h1>Heading</h1></div>
        <ul id="toc-list"></ul>
        <div id="slideover"></div>
        <div id="slideover-overlay"></div>
        <button id="slideover-close"></button>
      `;
      document.body.style.overflow = 'hidden';

      initialize(mockDotNetReference, {
        contentId: 'content',
        tocListId: 'toc-list',
        slideoverId: 'slideover',
        slideoverOverlayId: 'slideover-overlay',
        slideoverCloseId: 'slideover-close'
      });

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

      // Slideover was not active — overflow should be unchanged.
      // Kills `slideover.classList.contains('active') → true` mutation.
      expect(document.body.style.overflow).toBe('hidden');
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
        <a href="/bylaws">See Bylaws</a>
      `;

      mockDotNetReference.invokeMethodAsync.mockClear();

      // Act
      initializeSlideoverContent('slideover-content');

      const link = slideoverContent.querySelector('a[href="/bylaws"]');
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(clickEvent);

      // Assert
      expect(mockDotNetReference.invokeMethodAsync).toHaveBeenCalledWith(
        'OnInternalLinkClicked',
        'bylaws'
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
      globalThis.history.replaceState(null, '', 'http://localhost/bylaws');

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
