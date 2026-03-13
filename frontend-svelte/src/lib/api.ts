type ApiResponse<T> = {
	success: boolean;
	message: string;
	body: T;
};

async function request<T>(path: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
	const isFormData = options.body instanceof FormData;

	const response = await fetch(path, {
		...options,
		credentials: 'include',
		headers: isFormData ? {} : {
			'Content-Type': 'application/json',
			...options.headers
		}
	});

	if (response.status === 401) {
		window.location.href = '/login';
		throw new Error('Unauthorized');
	}

	if (!response.ok) {
		throw new Error(`${response.status} ${response.statusText}`);
	}

	return response.json();
}

export const api = {
	get: <T>(path: string) => 
		request<T>(path),
	post: <T>(path: string, body?: unknown) =>
		request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
	put: <T>(path: string, body?: unknown) =>
		request<T>(path, { method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
	patch: <T>(path: string, body?: unknown) =>
		request<T>(path, { method: 'PATCH', body: body ? JSON.stringify(body) : undefined }),
	delete: <T>(path: string) => 
		request<T>(path, { method: 'DELETE' }),
	form: <T>(path:string, body: FormData) => 
		request<T>(path, { method: 'POST', body }),
	postBinary: <T>(path: string, body: Blob) =>
		request<T>(path, { method: 'POST', body, headers: { 'Content-Type': 'application/octet-stream' }}),
};
