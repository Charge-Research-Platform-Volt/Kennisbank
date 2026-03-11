export function formatDate(dateString: string, precision: 'Year' | 'Month' | 'Day' = 'Day'): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    if (precision === 'Year') return date.getFullYear().toString();
    if (precision === 'Month') return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    return date.toLocaleDateString();
}