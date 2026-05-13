window.lock = () => document.body.style.overflow = 'hidden';
window.unlock = () => document.body.style.overflow = '';

window.prefersDarkScheme = () => {
    try {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch {
        return false;
    }
};

function togglePassword(inputElement) {
    if (!inputElement) return;
    inputElement.type = inputElement.type === 'password' ? 'text' : 'password';
}
