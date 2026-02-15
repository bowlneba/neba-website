/**
 * NebaDocument - Reusable component for displaying documents with table of contents.
 * Provides TOC generation, scroll spy, smooth scrolling, hash navigation, and
 * internal link interception via Blazor callback.
 */

let dotNetReference = null;
let isInitialized = false;
let abortController = null;

/**
 * Escapes HTML special characters to prevent XSS attacks.
 * @param {string} text - The text to escape
 * @returns {string} The escaped text safe for HTML insertion
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Validates required DOM elements exist.
 * @param {HTMLElement|null} content - Content container element
 * @param {string} contentId - Content element ID (for error reporting)
 * @returns {boolean} True if validation passed
 */
function validateElements(content, contentId) {
    if (!content) {
        console.error('[NebaDocument] Content element not found:', contentId);
        return false;
    }
    return true;
}

/**
 * Generates TOC HTML from headings and populates both desktop and mobile TOC containers.
 * @param {NodeList} headings - Collection of heading elements
 * @param {HTMLElement|null} tocList - Desktop TOC list container
 * @param {HTMLElement|null} tocMobileList - Mobile TOC list container
 * @returns {boolean} True if TOC was generated
 */
function generateAndPopulateToc(headings, tocList, tocMobileList) {
    if (!tocList || headings.length === 0) {
        return false;
    }

    let tocHtml = '<ul class="toc-list">';

    headings.forEach((heading, index) => {
        if (!heading.id) {
            heading.id = `heading-${index}`;
        }

        const level = heading.tagName.toLowerCase();
        const text = escapeHtml(heading.textContent);
        const className = level === 'h1' ? 'toc-item-h1' : `toc-item-${level}`;

        tocHtml += `<li class="${className}">
            <a href="#${heading.id}" class="toc-link" data-target="${heading.id}">${text}</a>
        </li>`;
    });

    tocHtml += '</ul>';
    tocList.innerHTML = tocHtml;

    if (tocMobileList) {
        tocMobileList.innerHTML = tocHtml;
    }

    return true;
}

/**
 * Sets up mobile TOC modal open/close interactions.
 * @param {HTMLElement|null} tocMobileButton - Button to open modal
 * @param {HTMLElement|null} tocModal - Modal container
 * @param {HTMLElement|null} tocModalOverlay - Modal overlay
 * @param {HTMLElement|null} tocModalClose - Close button
 * @param {HTMLElement|null} tocMobileList - Mobile TOC list
 * @param {AbortSignal} signal - Abort signal for cleanup
 */
function setupMobileModal(tocMobileButton, tocModal, tocModalOverlay, tocModalClose, tocMobileList, signal) {
    if (!tocMobileButton || !tocModal || !tocModalOverlay || !tocModalClose) {
        return;
    }

    const closeModal = () => {
        tocModal.classList.remove('active');
        document.body.style.overflow = '';
    };

    tocMobileButton.addEventListener('click', () => {
        tocModal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }, { signal });

    tocModalClose.addEventListener('click', closeModal, { signal });
    tocModalOverlay.addEventListener('click', closeModal, { signal });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && tocModal.classList.contains('active')) {
            closeModal();
        }
    }, { signal });

    if (tocMobileList) {
        setupMobileTocLinkHandlers(tocMobileList, closeModal, signal);
    }
}

/**
 * Sets up click handlers for mobile TOC links — scrolls to heading and closes modal.
 * @param {HTMLElement} tocMobileList - Mobile TOC list
 * @param {Function} closeModal - Function to close modal
 * @param {AbortSignal} signal - Abort signal for cleanup
 */
function setupMobileTocLinkHandlers(tocMobileList, closeModal, signal) {
    const mobileTocLinks = tocMobileList.querySelectorAll('.toc-link');

    mobileTocLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.dataset.target;
            const targetElement = document.getElementById(targetId);

            if (targetElement) {
                closeModal();
                setTimeout(() => {
                    const navbarHeight = 80;
                    const offsetPosition = targetElement.getBoundingClientRect().top + globalThis.pageYOffset - navbarHeight;
                    globalThis.scrollTo({ top: offsetPosition, behavior: 'smooth' });
                }, 300);
            }
        }, { signal });
    });
}

/**
 * Sets up scroll spy: highlights the active TOC entry as the user scrolls.
 * @param {HTMLElement} content - Content container
 * @param {HTMLElement|null} tocList - TOC list container
 * @param {NodeList} headings - Collection of heading elements
 * @param {AbortSignal} signal - Abort signal for cleanup
 */
function setupScrollSpy(content, tocList, headings, signal) {
    if (!tocList) {
        return;
    }

    const tocLinks = tocList.querySelectorAll('.toc-link');
    let currentActiveLink = null;

    function updateActiveLink() {
        const contentRect = content.getBoundingClientRect();
        const activeHeading = findActiveHeading(headings, contentRect);

        if (activeHeading) {
            const newActiveLink = tocList.querySelector(`[data-target="${activeHeading.id}"]`);
            if (newActiveLink !== currentActiveLink) {
                currentActiveLink?.classList.remove('active');
                newActiveLink?.classList.add('active');
                currentActiveLink = newActiveLink;
                scrollTocToActiveLink(newActiveLink);
            }
        }
    }

    tocLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.dataset.target;
            const targetElement = document.getElementById(targetId);

            if (targetElement) {
                const contentRect = content.getBoundingClientRect();
                const targetRect = targetElement.getBoundingClientRect();
                const offset = 20;
                const scrollPosition = content.scrollTop + (targetRect.top - contentRect.top) - offset;

                content.scrollTo({ top: scrollPosition, behavior: 'smooth' });
                scrollTocToActiveLink(link);
            }
        }, { signal });
    });

    let ticking = false;
    content.addEventListener('scroll', () => {
        if (!ticking) {
            globalThis.requestAnimationFrame(() => {
                updateActiveLink();
                ticking = false;
            });
            ticking = true;
        }
    }, { signal });

    updateActiveLink();
}

/**
 * Finds the heading currently visible near the top of the content viewport.
 * @param {NodeList} headings - Collection of heading elements
 * @param {DOMRect} contentRect - Bounding rect of content container
 * @returns {HTMLElement|null} The active heading element
 */
function findActiveHeading(headings, contentRect) {
    let activeHeading = null;
    let minDistance = Infinity;

    headings.forEach(heading => {
        const headingRect = heading.getBoundingClientRect();
        const distanceFromTop = headingRect.top - contentRect.top;

        if (distanceFromTop <= 100 && distanceFromTop >= -headingRect.height) {
            if (Math.abs(distanceFromTop) < minDistance) {
                minDistance = Math.abs(distanceFromTop);
                activeHeading = heading;
            }
        }
    });

    if (!activeHeading) {
        headings.forEach(heading => {
            const headingRect = heading.getBoundingClientRect();
            const distanceFromTop = headingRect.top - contentRect.top;

            if (distanceFromTop >= 0 && distanceFromTop < minDistance) {
                minDistance = distanceFromTop;
                activeHeading = heading;
            }
        });
    }

    return activeHeading;
}

/**
 * Scrolls the TOC sidebar so the active link is visible.
 * @param {HTMLElement|null} link - The TOC link element to scroll to
 */
function scrollTocToActiveLink(link) {
    const tocContainer = link?.closest('.toc-sticky');
    if (!tocContainer || !link) return;

    const tocRect = tocContainer.getBoundingClientRect();
    const linkRect = link.getBoundingClientRect();
    const linkTop = linkRect.top - tocRect.top + tocContainer.scrollTop;
    const targetScroll = linkTop - 60;

    tocContainer.scrollTo({ top: Math.max(0, targetScroll), behavior: 'smooth' });
}

/**
 * Builds a lookup map from any existing element IDs (e.g., Google Docs anchors like
 * "h.xk7tre4v41xy") found on heading elements to their generated anchor IDs.
 * This allows hash-fragment links using original IDs to resolve to the correct heading.
 * @param {NodeList} headings - Collection of heading elements
 * @returns {Map<string, string>} Map from original/alternative IDs to current heading IDs
 */
function buildAnchorLookup(headings) {
    const lookup = new Map();

    headings.forEach(heading => {
        if (heading.id) {
            lookup.set(heading.id, heading.id);
        }

        // Map original Google Docs IDs (preserved as data-original-id by HtmlProcessor)
        const originalId = heading.dataset.originalId;
        if (originalId) {
            lookup.set(originalId, heading.id);
        }

        // Google Docs may also embed <span> or <a> children with IDs
        const childAnchors = heading.querySelectorAll('[id]');
        childAnchors.forEach(child => {
            if (child.id && child.id !== heading.id) {
                lookup.set(child.id, heading.id);
            }
        });
    });

    return lookup;
}

/**
 * Sets up click interception for internal document links.
 * Anchor links (#hash) scroll within the content. Same-origin links that point to
 * the current page with a hash scroll within content. Cross-page same-origin links
 * invoke a Blazor callback so the parent page can load the document via the API pipeline.
 * @param {HTMLElement} content - The document content container
 * @param {NodeList} headings - Collection of heading elements (for anchor lookup)
 * @param {string|null} slideoverId - ID of the slideover container
 * @param {string|null} slideoverOverlayId - ID of the slideover overlay
 * @param {string|null} slideoverCloseId - ID of the slideover close button
 * @param {AbortSignal} signal - Abort signal for cleanup
 */
function setupInternalLinkNavigation(content, headings, slideoverId, slideoverOverlayId, slideoverCloseId, signal) {
    const slideover = slideoverId ? document.getElementById(slideoverId) : null;
    const slideoverOverlay = slideoverOverlayId ? document.getElementById(slideoverOverlayId) : null;
    const slideoverClose = slideoverCloseId ? document.getElementById(slideoverCloseId) : null;

    if (slideover && slideoverOverlay && slideoverClose) {
        const closeSlideover = () => {
            slideover.classList.remove('active');
            document.body.style.overflow = '';
        };

        slideoverClose.addEventListener('click', closeSlideover, { signal });
        slideoverOverlay.addEventListener('click', closeSlideover, { signal });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && slideover.classList.contains('active')) {
                closeSlideover();
            }
        }, { signal });
    }

    const anchorLookup = buildAnchorLookup(headings);
    const contentLinks = content.querySelectorAll('a[href]');
    const currentPath = globalThis.location.pathname;

    contentLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            const href = link.getAttribute('href');
            if (!href || href === '#') return;
            if (e.ctrlKey || e.metaKey) return;

            // Hash-only links: scroll within the document
            if (href.startsWith('#')) {
                e.preventDefault();
                handleAnchorNavigation(content, href, anchorLookup);
                return;
            }

            try {
                const linkUrl = new URL(href, globalThis.location.href);
                const isInternal = linkUrl.origin === globalThis.location.origin;
                const isExternalProtocol = linkUrl.protocol === 'mailto:' || linkUrl.protocol === 'tel:';

                if (!isInternal || isExternalProtocol) return;

                // Same-page link with hash: scroll within content instead of navigating
                if (linkUrl.pathname === currentPath && linkUrl.hash) {
                    e.preventDefault();
                    handleAnchorNavigation(content, linkUrl.hash, anchorLookup);
                    return;
                }

                // Different-page internal link: open in slideover via Blazor callback
                if (dotNetReference && slideover) {
                    e.preventDefault();
                    const pathname = linkUrl.pathname.replace(/^\//, '');
                    slideover.classList.add('active');
                    document.body.style.overflow = 'hidden';
                    dotNetReference.invokeMethodAsync('OnInternalLinkClicked', pathname);
                }
            } catch {
                // Let the browser handle malformed URLs normally
            }
        }, { signal });
    });
}

/**
 * Handles anchor (#hash) navigation within the current document.
 * Uses the anchor lookup to resolve Google Docs IDs (e.g., "h.xk7tre4v41xy")
 * to generated heading IDs (e.g., "article-1-name-purpose").
 * @param {HTMLElement} content - The document content container
 * @param {string} href - The hash href (e.g., "#section-1" or "#h.xk7tre4v41xy")
 * @param {Map<string, string>} anchorLookup - Map from original IDs to heading IDs
 */
function handleAnchorNavigation(content, href, anchorLookup) {
    const rawId = href.substring(1);

    // Resolve through lookup (handles Google Docs IDs → generated kebab-case IDs)
    const resolvedId = anchorLookup.get(rawId) ?? rawId;
    const targetElement = document.getElementById(resolvedId) ?? document.getElementById(rawId);

    if (!targetElement) return;

    const isContentScrollable =
        (content.scrollHeight > content.clientHeight) ||
        (content.scrollHeight === 0 && content.scrollTo);

    if (isContentScrollable) {
        const contentRect = content.getBoundingClientRect();
        const targetRect = targetElement.getBoundingClientRect();
        const offset = 20;
        const scrollPosition = content.scrollTop + (targetRect.top - contentRect.top) - offset;
        content.scrollTo({ top: scrollPosition, behavior: 'smooth' });
    } else {
        const navbarHeight = 80;
        const offset = 10;
        const targetPosition = targetElement.getBoundingClientRect().top + globalThis.scrollY - navbarHeight - offset;
        globalThis.scrollTo({ top: targetPosition, behavior: 'smooth' });
    }

    // Use replaceState instead of location.hash to avoid triggering Blazor's router
    const newHash = `#${resolvedId}`;
    if (globalThis.location.hash !== newHash) {
        history.replaceState(null, '', newHash);
    }
}

/**
 * Initialize the table-of-contents, scroll spy, mobile modal, and internal link handling.
 * @param {DotNetObjectReference} dotNetRef - Reference to the Blazor component for callbacks
 * @param {Object} config - Configuration object with element IDs
 * @returns {boolean} True if initialization succeeded
 */
export function initialize(dotNetRef, config) {
    if (isInitialized) {
        dispose();
    }

    dotNetReference = dotNetRef;
    abortController = new AbortController();
    const { signal } = abortController;

    const {
        contentId,
        tocListId,
        tocMobileListId,
        tocMobileButtonId,
        tocModalId,
        tocModalOverlayId,
        tocModalCloseId,
        headingLevels = 'h1, h2',
        slideoverId = null,
        slideoverOverlayId = null,
        slideoverCloseId = null
    } = config || {};

    const content = document.getElementById(contentId);

    if (!validateElements(content, contentId)) {
        return false;
    }

    const tocList = document.getElementById(tocListId);
    const tocMobileList = document.getElementById(tocMobileListId);
    const tocMobileButton = document.getElementById(tocMobileButtonId);
    const tocModal = document.getElementById(tocModalId);
    const tocModalOverlay = document.getElementById(tocModalOverlayId);
    const tocModalClose = document.getElementById(tocModalCloseId);

    const headings = content.querySelectorAll(headingLevels);

    if (tocList && headings.length > 0) {
        generateAndPopulateToc(headings, tocList, tocMobileList);
        setupMobileModal(tocMobileButton, tocModal, tocModalOverlay, tocModalClose, tocMobileList, signal);
        setupScrollSpy(content, tocList, headings, signal);
    }

    // Always set up link navigation — even when no TOC headings are found,
    // internal links still need to be intercepted to prevent Blazor router navigation.
    setupInternalLinkNavigation(content, headings, slideoverId, slideoverOverlayId, slideoverCloseId, signal);

    isInitialized = true;
    return true;
}

/**
 * Scrolls to a URL hash target within the content and updates the active TOC link.
 * @param {string} contentId - ID of the content container
 * @param {string} tocListId - ID of the TOC list container
 */
export function scrollToHash(contentId, tocListId) {
    const hash = globalThis.location.hash;
    if (!hash) return;

    const targetId = hash.substring(1);
    const content = document.getElementById(contentId);
    const targetElement = document.getElementById(targetId);

    if (!content || !targetElement) return;

    const isContentScrollable =
        (content.scrollHeight > content.clientHeight) ||
        (content.scrollHeight === 0 && content.scrollTo);

    if (isContentScrollable) {
        const contentRect = content.getBoundingClientRect();
        const targetRect = targetElement.getBoundingClientRect();
        const offset = 20;
        const scrollPosition = content.scrollTop + (targetRect.top - contentRect.top) - offset;
        content.scrollTo({ top: scrollPosition, behavior: 'smooth' });
    } else {
        const navbarHeight = 80;
        const offset = 10;
        const targetPosition = targetElement.getBoundingClientRect().top + globalThis.scrollY - navbarHeight - offset;
        globalThis.scrollTo({ top: targetPosition, behavior: 'smooth' });
    }

    const tocList = document.getElementById(tocListId);
    if (tocList) {
        const activeLink = tocList.querySelector('.toc-link.active');
        const newActiveLink = tocList.querySelector(`[data-target="${targetId}"]`);

        activeLink?.classList.remove('active');
        newActiveLink?.classList.add('active');

        if (newActiveLink) {
            scrollTocToActiveLink(newActiveLink);
        }
    }
}

/**
 * Opens the slideover panel.
 * @param {string} slideoverId - ID of the slideover container
 */
export function openSlideover(slideoverId) {
    const slideover = document.getElementById(slideoverId);
    if (slideover) {
        slideover.classList.add('active');
        document.body.style.overflow = 'hidden';
    }
}

/**
 * Closes the slideover panel.
 * @param {string} slideoverId - ID of the slideover container
 */
export function closeSlideover(slideoverId) {
    const slideover = document.getElementById(slideoverId);
    if (slideover) {
        slideover.classList.remove('active');
        document.body.style.overflow = '';
    }
}

/** Clean up all event listeners and references. */
export function dispose() {
    if (abortController) {
        abortController.abort();
        abortController = null;
    }

    dotNetReference = null;
    isInitialized = false;
}
