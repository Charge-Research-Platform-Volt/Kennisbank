import { api } from '$lib/api';

function makeValidUrl(input: string): string {
	if (input.startsWith('https://') || input.startsWith('http://')) return input;
	return 'https://' + input;
}

export async function openFile(id: string, fileType: string): Promise<void> {
	if (fileType === 'website') {
		const result = await api.get<{ url: string }>(
			`/api/resources/info/${id}?properties=${encodeURIComponent('WebsiteMetadata.Url')}`
		);
		window.open(makeValidUrl(result.body.url), '_blank');
	} else {
		window.open(`/api/files/download/${id}`, '_blank');
	}
}
