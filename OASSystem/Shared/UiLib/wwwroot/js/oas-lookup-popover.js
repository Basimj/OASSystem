// Close the native HTML popover explicitly before the Blazor model re-renders.
// The browser popover target attributes remain as a no-JS fallback.
export function closePopover(popoverId) {
    const popover = document.getElementById(popoverId);
    if (popover && typeof popover.hidePopover === "function" && popover.matches(":popover-open")) {
        popover.hidePopover();
    }
}
