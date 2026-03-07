/**
 * Sets body overflow to prevent or restore scrolling when a modal is open.
 * @param {boolean} hidden - True to prevent scrolling, false to restore it.
 */
export function setBodyOverflow(hidden) {
    document.body.style.overflow = hidden ? 'hidden' : '';
}
