// A native modal dialog enters the top layer, so report overflow cannot clip it.
export function open(dialog) {
    if (!dialog.open) dialog.showModal();
    dialog.querySelector('input[type="search"]')?.focus();
}

export function close(dialog) {
    if (dialog.open) dialog.close();
}
