// Tailwind CSS CDN configuration for local development
//
// NOTE: This file uses browser-compatible syntax for the Tailwind CDN.
// For production builds, use a Node.js-style config (module.exports) at the project root
// with the Tailwind CLI to generate optimized, tree-shaken CSS.
//
// IMPORTANT: Breakpoint values must stay in sync with:
// - neba_theme.css (:root CSS variables)
// - breakpoints.js (JavaScript constants)

// Create the tailwind object if it doesn't exist (CDN hasn't loaded yet)
window.tailwind = window.tailwind || {};

window.tailwind.config = {
    darkMode: 'class',
    theme: {
        extend: {
            colors: {
                'neba-blue': {
                    700: 'var(--neba-blue-700)',
                    600: 'var(--neba-blue-600)',
                    500: 'var(--neba-blue-500)',
                    300: 'var(--neba-blue-300)',
                    100: 'var(--neba-blue-100)',
                    brand: 'var(--neba-blue-brand)',
                },
                'neba-gray': {
                    50: 'var(--neba-gray-050)',
                    100: 'var(--neba-gray-100)',
                    200: 'var(--neba-gray-200)',
                    300: 'var(--neba-gray-300)',
                    400: 'var(--neba-gray-400)',
                    700: 'var(--neba-gray-700)',
                    800: 'var(--neba-gray-800)',
                },
                'neba-accent-red': 'var(--neba-accent-red)',
                'neba-success': 'var(--neba-success)',
                'neba-warning': 'var(--neba-warning)',
                'neba-info': 'var(--neba-info)',
                'neba-bg': 'var(--neba-bg)',
                'neba-bg-panel': 'var(--neba-bg-panel)',
                'neba-text': 'var(--neba-text)',
                'neba-border': 'var(--neba-border)',
            },
            borderRadius: {
                'neba': 'var(--neba-radius)',
                'neba-lg': 'var(--neba-radius-lg)',
            },
            maxWidth: {
                'neba-content': 'var(--neba-content-max-width)',
            },
            screens: {
                'mobile': { 'max': '767px' },
                'tablet': { 'min': '768px', 'max': '1100px' },
                'desktop-tight': { 'min': '1101px', 'max': '1250px' },
                'desktop-medium': { 'min': '1251px', 'max': '1400px' },
                'desktop-wide': { 'min': '1401px' },
            },
        },
    },
};
