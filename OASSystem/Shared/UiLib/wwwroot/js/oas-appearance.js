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
// Preset-specific surface tokens. The user's saved chrome and accent colors
// remain the source of truth; the preset controls the page palette everywhere.
const surfacePalettes = {
    Ocean: {
        bg: '#F1FBFD', surface: '#FFFFFF', subtle: '#E9F7FA',
        text: '#21485C', muted: '#5F7885', border: '#C8E5EE',
        borderSubtle: '#E5F2F6', table: '#E4F4F8',
        warningBg: '#FFF4E2', warningInk: '#80500B', infoBg: '#E5F5FB'
    },
    SkyYellow: {
        bg: '#F4FAFF', surface: '#FFFFFF', subtle: '#EDF6FE',
        text: '#173B59', muted: '#496B84', border: '#D5E8F4',
        borderSubtle: '#E9F3F9', table: '#E8F5FF',
        warningBg: '#FFF1B8', warningInk: '#765400', infoBg: '#E7F4FE'
    },
    Slate: {
        bg: '#F5F8FC', surface: '#FFFFFF', subtle: '#EEF3F9',
        text: '#273B52', muted: '#64748B', border: '#D7E0EB',
        borderSubtle: '#EBF0F6', table: '#EAF0F7',
        warningBg: '#FFF5DF', warningInk: '#8B5D13', infoBg: '#EAF2FA'
    },
    Custom: {
        bg: '#F8FAFD', surface: '#FFFFFF', subtle: '#F3F7FB',
        text: '#253849', muted: '#607387', border: '#DFE8F0',
        borderSubtle: '#EDF3F7', table: '#EFF5FA',
        warningBg: '#FFF4E5', warningInk: '#8E570B', infoBg: '#ECF5FB'
    }
};
const charcoal = {
    bg: '#0E0F12', surface: '#191B1F', subtle: '#22252A',
    input: '#22252A', text: '#F3F5F7', muted: '#BAC2CD',
    border: '#383E46', borderSubtle: '#30353D', table: '#262B31',
    warningBg: '#41331F', warningInk: '#F2C27D', infoBg: '#20333C'
};
function blendHex(a,b,amount) {
    const out = [1,3,5].map(i => Math.round(parseInt(a.slice(i,i+2),16)*(1-amount) + parseInt(b.slice(i,i+2),16)*amount));
    return `#${out.map(v=>v.toString(16).padStart(2,'0')).join('')}`.toUpperCase();
}
function contrast(a,b) {
    const l1=luminance(a), l2=luminance(b);
    return (Math.max(l1,l2)+.05)/(Math.min(l1,l2)+.05);
}
function readableInk(bg,wanted,min=4.5) {
    const requested=safeHex(wanted,'#173B59');
    if (contrast(bg,requested)>=min) return requested;
    return contrast(bg,'#142F40') >= contrast(bg,'#FFFFFF') ? '#142F40' : '#FFFFFF';
}
function primaryInk(hex) { return readableInk(hex, '#FFFFFF'); }
function ensureKeys(order) {
    if (!Array.isArray(order) || order.length !== 3 || new Set(order).size !== 3 || !order.every(x => permittedHeaderKeys.includes(x)))
        return permittedHeaderKeys;
    return order;
}
function notifyTheme(mode) {
    if (dotNetRef) dotNetRef.invokeMethodAsync('ThemeModeChanged', mode).catch(() => {});
}
function notifySidebar(mode) {
    if (dotNetRef) dotNetRef.invokeMethodAsync('AppearanceChanged', mode).catch(() => {});
}
export function apply(settings, notify = true) {
    const el = shell();
    if (!settings || !el) return;
    const preset = settings.themePreset === 'SkyYellow' ? 'SkyYellow'
        : settings.themePreset === 'Slate' ? 'Slate'
        : settings.themePreset === 'Custom' ? 'Custom' : 'Ocean';
    const darkMode = settings.themeMode === 'Dark';
    const defaultAccent = preset === 'SkyYellow' ? '#0B6FAF'
        : preset === 'Slate' ? '#526C8B' : '#0B8095';
    const accent = safeHex(settings.primaryColor, defaultAccent);
    // Custom accent picks very faint coordinating surfaces rather than
    // retaining blue or teal backgrounds from whichever preset was used last.
    const palette = darkMode ? charcoal : preset === 'Custom'
        ? {...surfacePalettes.Custom,
            bg: blendHex(accent,'#FFFFFF',.964),
            subtle: blendHex(accent,'#FFFFFF',.940),
            table: blendHex(accent,'#FFFFFF',.915),
            border: blendHex(accent,'#FFFFFF',.815),
            borderSubtle: blendHex(accent,'#FFFFFF',.936)}
        : surfacePalettes[preset];
    // New accounts and theme resets are bright. Older Ocean users with the
    // retired petrol default are upgraded without replacing custom colors.
    const oldOceanSidebar = preset === 'Ocean' &&
        ['#28596c','#1b4659'].includes((settings.sidebarBackground ?? '').toLowerCase());
    const sidebarColor = oldOceanSidebar ? '#DDF3F8'
        : safeHex(settings.sidebarBackground, '#DDF3F8');
    const sidebarText = oldOceanSidebar && ['#ffffff'].includes((settings.sidebarTextColor ?? '').toLowerCase())
        ? '#174558' : settings.sidebarTextColor;
    const sidebarIcon = oldOceanSidebar && (settings.sidebarIconColor ?? '').toLowerCase() === '#d3eaf0'
        ? '#28667B' : settings.sidebarIconColor;
    const sidebarHover = oldOceanSidebar ? '#C8EAF2'
        : safeHex(settings.sidebarHoverBackground, '#C8EAF2');
    const sidebarSelected = oldOceanSidebar ? '#B9E5F1'
        : safeHex(settings.sidebarActiveBackground, '#B9E5F1');
    const sidebarSelectedText = oldOceanSidebar ? '#123D56' : settings.sidebarActiveTextColor;
    const headerColor = safeHex(settings.headerBackground,'#FFFFFF');
    const tabColor = safeHex(settings.tabsBarBackground,'#FFFFFF');
    const tabsBg = safeHex(settings.tabsBackground,'#FFFFFF');
    const tabsSelected = safeHex(settings.tabsActiveBackground,'#D7F1F8');

    // A dark mode is a complete charcoal palette, never a bright sidebar above
    // dark content. Switching back reconstructs all colors from saved prefs.
    const darkChrome = {
        sidebar: '#20252B', sidebarFg: '#F2F5F7', sidebarIcon:'#C7D7DC',
        sidebarHover: '#303A43', sidebarActive: '#354A51',
        sidebarActiveFg: '#FFFFFF', sidebarActiveIcon: '#8DE0CA',
        header: '#1C2127', headerFg: '#F2F5F7', headerIcon: '#CFD8E0',
        tabBar: '#1C2127', tabBg: '#252B32', tabFg: '#D9E1E7',
        tabIcon: '#C9D6E0', tabActive: '#364047', tabActiveFg: '#FFFFFF',
        tabActiveIcon: '#9BE2E0', tabBorder: '#414A54'
    };
    const chrome = darkMode ? darkChrome : {
        sidebar: sidebarColor,
        sidebarFg: readableInk(sidebarColor, sidebarText),
        sidebarIcon: readableInk(sidebarColor, sidebarIcon, 3),
        sidebarHover,
        sidebarActive: sidebarSelected,
        sidebarActiveFg: readableInk(sidebarSelected, sidebarSelectedText),
        sidebarActiveIcon: readableInk(sidebarSelected, oldOceanSidebar ? '#086C87' : settings.sidebarActiveIconColor, 3),
        header: headerColor,
        headerFg: readableInk(headerColor, settings.headerTextColor),
        headerIcon: readableInk(headerColor, settings.headerIconColor, 3),
        tabBar: tabColor,
        tabBg: tabsBg,
        tabFg: readableInk(tabsBg, settings.tabsTextColor),
        tabIcon: readableInk(tabsBg, settings.tabsIconColor, 3),
        tabActive: tabsSelected,
        tabActiveFg: readableInk(tabsSelected, settings.tabsActiveTextColor),
        tabActiveIcon: readableInk(tabsSelected, settings.tabsActiveIconColor, 3),
        tabBorder: safeHex(settings.tabsBorderColor, palette.border)
    };
    const primarySoft = blendHex(accent, darkMode ? '#20262D' : '#FFFFFF', darkMode ? .79 : .90);
    const primaryBorder = blendHex(accent, darkMode ? '#485862' : '#FFFFFF', darkMode ? .48 : .67);
    const primaryHover = blendHex(accent, darkMode ? '#FFFFFF' : '#000000', darkMode ? .13 : .16);
    const semanticVars = {
        '--oas-ui-accent': accent,
        '--oas-ui-accent-fg': primaryInk(accent),
        '--oas-color-on-primary': primaryInk(accent),
        '--oas-color-primary': accent,
        '--oas-color-primary-hover': primaryHover,
        '--oas-color-primary-soft': primarySoft,
        '--oas-color-primary-border': primaryBorder,
        '--oas-color-background': palette.bg,
        '--oas-color-surface': palette.surface,
        '--oas-color-subtle': palette.subtle,
        '--oas-color-hover': darkMode ? '#292F36' : blendHex(accent,'#FFFFFF',.94),
        '--oas-color-disabled-surface': darkMode ? '#292C31' : '#F1F4F5',
        '--oas-color-text': palette.text,
        '--oas-color-text-muted': palette.muted,
        '--oas-color-border': palette.border,
        '--oas-color-border-subtle': palette.borderSubtle,
        '--oas-color-card-border': palette.border,
        '--oas-color-card-shadow': darkMode ? '0 4px 18px rgba(0,0,0,.16)' : '0 4px 18px rgba(39,79,99,.055)',
        '--oas-color-table-header': palette.table,
        '--oas-color-table-row-hover': darkMode ? '#292F36' : blendHex(accent,palette.surface,.94),
        '--oas-color-table-row-selected': darkMode ? '#343E45' : (preset === 'SkyYellow' ? '#FFF1B8' : blendHex(accent,palette.surface,.87)),
        '--oas-color-warning': palette.warningInk,
        '--oas-color-warning-surface': palette.warningBg,
        '--oas-color-info-surface': palette.infoBg,
        '--oas-color-info-border': darkMode ? '#46616C' : palette.border,
        '--oas-color-success': darkMode ? '#6EE2AD' : '#087E69',
        '--oas-color-success-surface': darkMode ? '#1B372D' : '#E7F8F1',
        '--oas-color-danger': darkMode ? '#FF848A' : '#C92F39',
        '--oas-color-danger-surface': darkMode ? '#3F272B' : '#FFF0F0',
        '--oas-color-focus-ring': darkMode ? 'rgba(100,205,180,.32)' : 'rgba(16,120,160,.24)',
        '--oas-ui-sidebar': chrome.sidebar,
        '--oas-ui-sidebar-fg': chrome.sidebarFg,
        '--oas-ui-sidebar-icon': chrome.sidebarIcon,
        '--oas-ui-sidebar-hover': chrome.sidebarHover,
        '--oas-ui-sidebar-active-bg': chrome.sidebarActive,
        '--oas-ui-sidebar-active-fg': chrome.sidebarActiveFg,
        '--oas-ui-sidebar-active-icon': chrome.sidebarActiveIcon,
        '--oas-ui-header': chrome.header,
        '--oas-ui-header-fg': chrome.headerFg,
        '--oas-ui-header-icon': chrome.headerIcon,
        '--oas-ui-tabs-bar-bg': chrome.tabBar,
        '--oas-ui-tabs-bg': chrome.tabBg,
        '--oas-ui-tabs-fg': chrome.tabFg,
        '--oas-ui-tabs-icon': chrome.tabIcon,
        '--oas-ui-tabs-active-bg': chrome.tabActive,
        '--oas-ui-tabs-active-fg': chrome.tabActiveFg,
        '--oas-ui-tabs-active-icon': chrome.tabActiveIcon,
        '--oas-ui-tabs-border': chrome.tabBorder,
        '--oas-color-sidebar-background': chrome.sidebar,
        '--oas-color-sidebar-border': chrome.tabBorder,
        '--oas-color-sidebar-hover': chrome.sidebarHover,
        '--oas-color-sidebar-active-background': chrome.sidebarActive,
        '--oas-color-sidebar-active-foreground': chrome.sidebarActiveFg,
        '--oas-color-topbar-background': chrome.header,
        '--oas-color-topbar-subtle': chrome.header,
        '--oas-color-topbar-border': chrome.tabBorder,
        '--oas-theme-content-bg': palette.bg,
        '--oas-theme-card-bg': palette.surface,
        '--oas-theme-input-bg': palette.input ?? palette.surface,
        '--oas-theme-text': palette.text,
        '--oas-theme-muted': palette.muted,
        '--oas-theme-border': palette.border,
        '--oas-acc-primary': accent,
        '--oas-acc-primary-light': primaryHover,
        '--oas-acc-card-bg': palette.surface,
        '--oas-acc-card-border': palette.border,
        '--oas-acc-card-hover-border': primaryBorder,
        '--oas-acc-muted': palette.muted,
        '--oas-acc-surface-bg': palette.bg,
        '--oas-sales-primary': accent,
        '--oas-sales-surface': palette.surface,
        '--oas-sales-bg': palette.bg,
        '--oas-sales-border': palette.border,
        '--oas-sales-muted': palette.muted,
        '--oas-dark-bg': charcoal.bg,
        '--oas-dark-panel': charcoal.surface,
        '--oas-dark-header': charcoal.table,
        '--oas-dark-ink': charcoal.text,
        '--oas-dark-border': charcoal.border,
        '--bs-body-bg': palette.bg,
        '--bs-body-color': palette.text,
        '--bs-secondary-color': palette.muted,
        '--bs-border-color': palette.border,
        '--bs-secondary-bg': palette.surface,
        '--bs-tertiary-bg': palette.subtle,
        '--bs-emphasis-color': palette.text,
        '--bs-table-bg': palette.surface,
        '--bs-table-color': palette.text,
        '--bs-table-border-color': palette.border,
        '--oas-ui-font': fontFamilies[settings.fontFamily] ?? fontFamilies.Cairo,
    };
    for (const [name,value] of Object.entries(semanticVars)) el.style.setProperty(name,value);
    const scale = Math.max(.85, Math.min(1.25, Number(settings.fontScale) || 1));
    el.style.setProperty('--oas-ui-font-scale', String(scale));
    // Shared type scale: compact ERP defaults while retaining the user font-size slider.
    el.style.setProperty('--oas-font-xs', `${(11 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-sm', `${(11.5 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-md', `${(12.5 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-font-lg', `${(13.5 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-sidebar-nav-font-size', `${(12.5 * scale).toFixed(2)}px`);
    el.style.setProperty('--oas-sidebar-child-font-size', `${(12 * scale).toFixed(2)}px`);
    el.classList.toggle('oas-theme-dark', darkMode);
    el.classList.toggle('oas-preset-sky-yellow', preset === 'SkyYellow');
    for (const name of ['Ocean','SkyYellow','Slate','Midnight','Custom'])
        el.classList.toggle(`oas-preset-${name.toLowerCase()}`, settings.themePreset === name);
    for (const name of ['Classic','Compact','Minimal']) el.classList.remove(`oas-appearance-header-${name.toLowerCase()}`);
    const layout = ['Classic', 'Compact', 'Minimal'].includes(settings.headerLayout) ? settings.headerLayout : 'Classic';
    el.classList.add(`oas-appearance-header-${layout.toLowerCase()}`);
    for (const name of ['classic','pills','underline']) el.classList.remove(`oas-appearance-tabs-${name}`);
    const tabsStyle = ['Classic','Pills','Underline'].includes(settings.tabsStyle) ? settings.tabsStyle : 'Classic';
    el.classList.add(`oas-appearance-tabs-${tabsStyle.toLowerCase()}`);
    for (const name of ['compact','normal','comfortable']) el.classList.remove(`oas-appearance-tabs-size-${name}`);
    const tabsSize = ['Compact','Normal','Comfortable'].includes(settings.tabsSize) ? settings.tabsSize : 'Compact';
    el.classList.add(`oas-appearance-tabs-size-${tabsSize.toLowerCase()}`);
    for (const node of el.querySelectorAll('.oas-top-header > .oas-header-user-wrap, .oas-top-header > .oas-header-quick-actions, .oas-top-header > .oas-header-system-meta')) {
        const key = node.classList.contains('oas-header-user-wrap') ? 'user'
            : node.classList.contains('oas-header-quick-actions') ? 'notifications' : 'session';
        node.style.order = String(ensureKeys(settings.headerOrder).indexOf(key));
    }
    if (notify && ['Expanded','Collapsed'].includes(settings.sidebarMode)) notifySidebar(settings.sidebarMode);
    notifyTheme(darkMode ? 'Dark' : 'Light');
    currentSettings = { ...settings };
}

async function getServerSettings() {
    const response = await fetch(endpoint, { credentials: 'same-origin', cache: 'no-store' });
    if (!response.ok) throw new Error('تعذر تحميل إعدادات المظهر.');
    return await response.json();
}

export async function initialize(reference) {
    dotNetRef = reference;
    window.oasAppearanceInterop = { persistSidebarMode, toggleThemeMode };
    try { apply(await getServerSettings()); }
    catch { /* Token-based Ocean defaults remain usable if the API is unavailable. */ }
}

// Keep the button in the global header in sync with the DB-backed appearance.
// Do not change any other preferences (including user-selected colors or font).
let themeSaveInFlight = false;
async function toggleThemeMode() {
    if (themeSaveInFlight) return currentSettings?.themeMode ?? 'Light';
    themeSaveInFlight = true;
    try {
        const original = currentSettings ?? await getServerSettings();
        const updated = { ...original, themeMode: original.themeMode === 'Dark' ? 'Light' : 'Dark' };
        const response = await fetch(endpoint, {
            method: 'PUT', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updated)
        });
        if (!response.ok) throw new Error('تعذر حفظ وضع العرض.');
        const confirmed = await response.json();
        apply(confirmed, false);
        return confirmed.themeMode === 'Dark' ? 'Dark' : 'Light';
    } finally {
        themeSaveInFlight = false;
    }
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
