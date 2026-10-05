async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
	const isFormData = options.body instanceof FormData;

	const response = await fetch(path, {
		...options,
		credentials: 'include',
		headers: isFormData
			? {}
			: {
					'Content-Type': 'application/json',
					...options.headers
				}
	});

	if (response.status === 401) {
		const target = window.location.pathname + window.location.search;
		window.location.href =
			target === '/' ? '/login' : `/login?redirect=${encodeURIComponent(target)}`;
		throw new Error('Unauthorized');
	}

	if (!response.ok) {
		const body = await response.json().catch(() => null);
		throw new Error(body?.detail ?? body?.message ?? `${response.status} ${response.statusText}`);
	}

	const text = await response.text();
	return (text ? JSON.parse(text) : null) as T;
}

export const api = {
	get: <T>(path: string, options?: RequestInit) => request<T>(path, options),
	post: <T>(path: string, body?: unknown, options?: RequestInit) =>
		request<T>(path, { ...options, method: 'POST', body: body ? JSON.stringify(body) : undefined }),
	put: <T>(path: string, body?: unknown, options?: RequestInit) =>
		request<T>(path, { ...options, method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
	patch: <T>(path: string, body?: unknown, options?: RequestInit) =>
		request<T>(path, {
			...options,
			method: 'PATCH',
			body: body ? JSON.stringify(body) : undefined
		}),
	delete: <T>(path: string, options?: RequestInit) =>
		request<T>(path, { ...options, method: 'DELETE' }),
	form: <T>(path: string, body: FormData, options?: RequestInit) =>
		request<T>(path, { ...options, method: 'POST', body }),
	postBinary: <T>(path: string, body: Blob, options?: RequestInit) =>
		request<T>(path, {
			...options,
			method: 'POST',
			body,
			headers: { 'Content-Type': 'application/octet-stream' }
		})
};
