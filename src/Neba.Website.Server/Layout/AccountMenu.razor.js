/**
 * Copies the given text to the clipboard.
 * @param {string} text - The text to copy.
 */
export function copyToClipboard(text) {
    return navigator.clipboard.writeText(text);
}
