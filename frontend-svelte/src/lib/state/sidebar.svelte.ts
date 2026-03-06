const STORAGE_KEY = 'sidebar-open';

let _open = $state(localStorage.getItem(STORAGE_KEY) !== 'false');

export const leftSidebar = 
{
    get open() { return _open; },
    set open(v: boolean) { _open = v; localStorage.setItem(STORAGE_KEY, String(v)) },
    toggle() { _open = !_open; localStorage.setItem(STORAGE_KEY, String(_open)); }
}