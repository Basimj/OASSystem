// Browser-side renderer only. User settings are never stored in localStorage:
// the authenticated SQL Server appearance API is the authoritative source.
const endpoint = 'api/identity/profile/appearance';
const permittedHeaderKeys = ['user', 'notifications', 'session'];
const fontFamilies = {
  'Cairo': '"Cairo", "Tajawal", "Segoe UI", Tahoma, sans-serif',
  'Tajawal': '"Tajawal", "Cairo", "Segoe UI", Tahoma, sans-serif',
  'IBM Plex Sans Arabic': '"IBM Plex Sans Arabic", "Cairo", Tahoma, sans-serif',
  'Segoe UI': '"Segoe UI", "Cairo", Tahoma, sans-serif'
};
let currentSettings = null;
let dotNetRef = null;

function shell() { return document.querySelector('.oas-shell'); }
function safeHex(value, fallback) { return /^#[\da-fA-F]{6}$/.test(value ?? '') ? value : fallback; }
function luminance(hex) {
    const channels = [1,3,5].map(i => parseInt(hex.slice(i, i + 2), 16) / 255)
        .map(v => v <= .04045 ? v / 12.92 : Math.pow((v + .055) / 1.055, 2.4));
    return channels[0] * .2126 + channels[1] * .7152 + channels[2] * .0722;
}
function primaryInk(hex) { return luminance(hex) >= .179 ? '#152D3C' : '#FFFFFF'; }
function ensureKeys(order) {
    if (!Array.isArray(order) || order.length !== 3 || new Set(order).size !== 3 || !order.every(x => permittedHeaderKeys.includes(x)))
        return permittedHeaderKeys;
    return order;
}
function notifySidebar(mode) {
    if (dotNetRef) dotNetRef.invokeMethodAsync('AppearanceChanged', mode).catch(() => {});
}
export function apply(settings, notify = true) {
    if (!settings || !shell()) return;
    const el = shell();
    const vars = {
        '--oas-ui-accent': safeHex(settings.primaryColor, '#459DB9'),
        '--oas-color-on-primary': primaryInk(safeHex(settings.primaryColor, '#459DB9')),
        '--oas-ui-sidebar': safeHex(settings.sidebarBackground, '#1B4659'),
        '--oas-ui-sidebar-fg': safeHex(settings.sidebarTextColor, '#FFFFFF'),
        '--oas-ui-sidebar-icon': safeHex(settings.sidebarIconColor, '#D6EAF1'),
        '--oas-ui-sidebar-hover': safeHex(settings.sidebarHoverBackground, '#335F72'),
        '--oas-ui-sidebar-active-bg': safeHex(settings.sidebarActiveBackground, '#287E9A'),
        '--oas-ui-sidebar-active-fg': safeHex(settings.sidebarActiveTextColor, '#FFFFFF'),
        '--oas-ui-sidebar-active-icon': safeHex(settings.sidebarActiveIconColor, '#FFFFFF'),
        '--oas-ui-header': safeHex(settings.headerBackground, '#FFFFFF'),
        '--oas-ui-header-fg': safeHex(settings.headerTextColor, '#1C3444'),
        '--oas-ui-header-icon': safeHex(settings.headerIconColor, '#526C87'),
        '--oas-ui-tabs-bar-bg': safeHex(settings.tabsBarBackground, '#FFFFFF'),
        '--oas-ui-tabs-bg': safeHex(settings.tabsBackground, '#FFFFFF'),
        '--oas-ui-tabs-fg': safeHex(settings.tabsTextColor, '#61647F'),
        '--oas-ui-tabs-icon': safeHex(settings.tabsIconColor, '#61647F'),
        '--oas-ui-tabs-active-bg': safeHex(settings.tabsActiveBackground, '#E8F6FA'),
        '--oas-ui-tabs-active-fg': safeHex(settings.tabsActiveTextColor, '#216781'),
        '--oas-ui-tabs-active-icon': safeHex(settings.tabsActiveIconColor, '#216781'),
        '--oas-ui-tabs-border': safeHex(settings.tabsBorderColor, '#B4DCE9'),
        '--oas-ui-font': fontFamilies[settings.fontFamily] ?? fontFamilies.Cairo,
        '--oas-ui-font-scale': String(Math.max(.85, Math.min(1.25, Number(settings.fontScale) || 1)))
    };
    for (const [key, value] of Object.entries(vars)) el.style.setProperty(key, value);
    // Prior versions stored a scale token but most CSS components never read it.
    // Publish scale-aware semantic tokens used by tables, form controls and side navigation.
    const scale = Math.max(.85, Math.min(1.25, Number(settings.fontScale) || 1));
    el.style.setProperty('--oas-ui-font-scale', String(scale));
    el.style.setProperty('--oas-font-xs', `${(11 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-sm', `${(12 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-md', `${(13 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-lg', `${(14.4 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-sidebar-nav-font-size', `${(13 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-sidebar-child-font-size', `${(12 * scale).toFixed(2)}px`);
    const darkMode = settings.themeMode === 'Dark';
    el.classList.toggle('oas-theme-dark', darkMode);
    // Dark mode uses neutral charcoal surfaces; accent stays configurable.
    // All page-level styles consume the tokens inherited from .oas-shell.
    el.style.setProperty('--oas-theme-content-bg', darkMode ? '#0E0F12' : '#EFF7FC');
    el.style.setProperty('--oas-theme-card-bg', darkMode ? '#191B1F' : '#FFFFFF');
    el.style.setProperty('--oas-theme-input-bg', darkMode ? '#22252A' : '#FFFFFF');
    el.style.setProperty('--oas-theme-text', darkMode ? '#F3F5F7' : '#243849');
    el.style.setProperty('--oas-theme-muted', darkMode ? '#B5BCC6' : '#63798C');
    el.style.setProperty('--oas-theme-border', darkMode ? '#353940' : '#DCE7EF');
    for (const name of ['Classic', 'Compact', 'Minimal']) el.classList.remove(`oas-appearance-header-${name.toLowerCase()}`);
    const layout = ['Classic', 'Compact', 'Minimal'].includes(settings.headerLayout) ? settings.headerLayout : 'Classic';
    el.classList.add(`oas-appearance-header-${layout.toLowerCase()}`);
    for (const name of ['classic', 'pills', 'underline']) el.classList.remove(`oas-appearance-tabs-${name}`);
    const tabsStyle = ['Classic', 'Pills', 'Underline'].includes(settings.tabsStyle) ? settings.tabsStyle : 'Classic';
    el.classList.add(`oas-appearance-tabs-${tabsStyle.toLowerCase()}`);
    for (const name of ['compact', 'normal', 'comfortable']) el.classList.remove(`oas-appearance-tabs-size-${name}`);
    const tabsSize = ['Compact', 'Normal', 'Comfortable'].includes(settings.tabsSize) ? settings.tabsSize : 'Compact';
    el.classList.add(`oas-appearance-tabs-size-${tabsSize.toLowerCase()}`);
    for (const node of el.querySelectorAll('.oas-top-header > .oas-header-user-wrap, .oas-top-header > .oas-header-quick-actions, .oas-top-header > .oas-header-system-meta')) {
        const key = node.classList.contains('oas-header-user-wrap') ? 'user'
            : node.classList.contains('oas-header-quick-actions') ? 'notifications' : 'session';
        node.style.order = String(ensureKeys(settings.headerOrder).indexOf(key));
    }
    if (notify && ['Expanded', 'Collapsed'].includes(settings.sidebarMode)) notifySidebar(settings.sidebarMode);
    currentSettings = { ...settings };
}

async function getServerSettings() {
    const response = await fetch(endpoint, { credentials: 'same-origin', cache: 'no-store' });
    if (!response.ok) throw new Error('تعذر تحميل إعدادات المظهر.');
    return await response.json();
}

export async function initialize(reference) {
    dotNetRef = reference;
    window.oasAppearanceInterop = { persistSidebarMode };
    try { apply(await getServerSettings()); }
    catch { /* Ocean defaults are intentionally retained on any API failure. */ }
}

async function persistSidebarMode(mode) {
    if (!currentSettings || !['Expanded', 'Collapsed'].includes(mode)) return;
    const updated = { ...currentSettings, sidebarMode: mode };
    try {
        const response = await fetch(endpoint, {
            method: 'PUT', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updated)
        });
        if (response.ok) currentSettings = await response.json();
    } catch { /* UI remains usable even if preferences cannot be persisted. */ }
}

export function detach() {
    dotNetRef = null;
    delete window.oasAppearanceInterop;
}
