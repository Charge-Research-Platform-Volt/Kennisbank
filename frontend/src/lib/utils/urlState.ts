import { replaceState } from '$app/navigation';

export function getParam(key: string, fallback = ''): string {
	return new URLSearchParams(window.location.search).get(key) ?? fallback;
}

export function getParamInt(key: string, fallback = 1): number {
	return Number(new URLSearchParams(window.location.search).get(key) ?? fallback);
}

export function getParamArray(key: string, fallback: string[] = []): string[] {
	const val = new URLSearchParams(window.location.search).get(key);
	return val ? val.split(',') : fallback;
}

export function pageHref(p: number): string {
	const params = new URLSearchParams(window.location.search);
	if (p <= 1) params.delete('page');
	else params.set('page', String(p));
	return `?${params}`;
}

export function setParams(params: Record<string, string | number | null>): void {
	const current = new URLSearchParams(window.location.search);
	for (const [key, value] of Object.entries(params)) {
		if (value === null || value === '' || (key === 'page' && value === 1)) {
			current.delete(key);
		} else {
			current.set(key, String(value));
		}
	}
	const qs = current.toString();
	replaceState(qs ? `?${qs}` : window.location.pathname, {});
}
