export function getThemePreference() {
    return localStorage.getItem('os-dark-mode') === 'true';
}

export function setThemePreference(isDark) {
    localStorage.setItem('os-dark-mode', isDark ? 'true' : 'false');
}
