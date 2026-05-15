export function isPasswordValid(password: string): boolean {
    return password.length >= 6 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /[0-9]/.test(password) && /[^a-zA-Z0-9]/.test(password);
}