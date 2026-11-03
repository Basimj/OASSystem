// The journal account list is anchored to a table cell but displayed in the
// browser's top layer. This prevents clipping by horizontal table overflow.
const active = new WeakMap();

export function open(anchorId, menu) {
    const anchor = document.getElementById(anchorId);
    if (!anchor || !menu) return;

    const previous = active.get(menu);
    if (previous) previous();

    if (typeof menu.showPopover === 'function' && !menu.matches(':popover-open')) {
        menu.showPopover();
    }

    const position = () => {
        const rect = anchor.getBoundingClientRect();
        const viewportWidth = document.documentElement.clientWidth;
        const viewportHeight = window.innerHeight;
        const width = Math.min(Math.max(rect.width, 310), viewportWidth - 16);
        const left = Math.max(8, Math.min(viewportWidth - width - 8, rect.right - width));
        const desiredHeight = 304;
        const below = viewportHeight - rect.bottom - 12;
        const above = rect.top - 12;
        const placeAbove = below < 190 && above > below;
        const availableHeight = Math.max(100, placeAbove ? above : below);
        const maxHeight = Math.min(desiredHeight, availableHeight);
        const top = placeAbove ? Math.max(8, rect.top - maxHeight - 4) : rect.bottom + 4;

        menu.style.left = `${left}px`;
        menu.style.right = 'auto';
        menu.style.top = `${top}px`;
        menu.style.bottom = 'auto';
        menu.style.width = `${width}px`;
        menu.style.maxHeight = `${maxHeight}px`;
    };

    const cleanup = () => {
        window.removeEventListener('resize', position);
        document.removeEventListener('scroll', position, true);
        menu.removeEventListener('toggle', onToggle);
        active.delete(menu);
    };
    const onToggle = e => {
        if (e.newState === 'closed') cleanup();
    };

    window.addEventListener('resize', position);
    document.addEventListener('scroll', position, true);
    menu.addEventListener('toggle', onToggle);
    active.set(menu, cleanup);
    position();
}

export function close(menu) {
    if (!menu) return;
    const cleanup = active.get(menu);
    if (cleanup) cleanup();
    if (typeof menu.hidePopover === 'function' && menu.matches(':popover-open')) {
        menu.hidePopover();
    }
}
