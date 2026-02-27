let _open = $state(true);

export const leftSidebar = 
{
    get open() { return _open; },
    set open(v: boolean) { _open = v; }
}