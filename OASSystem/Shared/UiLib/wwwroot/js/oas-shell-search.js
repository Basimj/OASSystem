// Keyboard shortcut for the shell's page-navigation search.
let handler = null;
export function attachSearchShortcut(inputId) {
    detachSearchShortcut();
    handler = event => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            const input = document.getElementById(inputId);
            if (input && !input.disabled) {
                event.preventDefault();
                input.focus();
                input.select();
            }
        }
    };
    window.addEventListener('keydown', handler);
}
export function detachSearchShortcut() {
    if (handler) window.removeEventListener('keydown', handler);
    handler = null;
}
