import { api } from '$lib/api';

function makeValidUrl(input: string): string {
	if (input.startsWith('https://') || input.startsWith('http://')) return input;
	return 'https://' + input;
}

export async function openFile(id: string, fileType: string, sourceUrl?: string): Promise<void> {
	if (fileType === 'website') {
		if (sourceUrl) {
			window.open(makeValidUrl(sourceUrl), '_blank');
		} else {
			const result = await api.get<{ sourceUrl: string }>(`/api/resources/${id}`);
			window.open(makeValidUrl(result.sourceUrl ?? ''), '_blank');
		}
	} else {
		window.open(`/api/files/${id}`, '_blank');
	}
}
