let open = $state(false);
let message = $state('');
let resolve: (value: boolean) => void;

export function confirm(msg: string): Promise<boolean> {
    message = msg;
    open = true;

    return new Promise(r => { resolve = r; });
}

export const confirmState = {
    get open() { return open; },
    get message() { return message; },
    accept() { open = false; resolve(true); },
    cancel() { open = false; resolve(false); },
};