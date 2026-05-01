window.lock = () => document.body.style.overflow = 'hidden';
window.unlock = () => document.body.style.overflow = '';


function togglePassword(inputElement) {
    if (!inputElement) return;
    inputElement.type = inputElement.type === 'password' ? 'text' : 'password';
}
