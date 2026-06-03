import { api } from './api';

export async function uploadFile(file: File): Promise<string> {
	const CHUNK_SIZE = 5242880; // 5 MB

	// Init
	const init = await api.post<{ objectName: string; uploadId: string }>('/api/files/upload/init', {
		FileName: file.name,
		FileSize: file.size
	});
	const { objectName, uploadId } = init;

	// Upload chunks
	const partETags: Record<number, string> = {};
	let part = 1;

	for (let offset = 0; offset < file.size; offset += CHUNK_SIZE) {
		const chunk = file.slice(offset, offset + CHUNK_SIZE);
		const result = await api.postBinary<{ eTag: string }>(
			`/api/files/upload/part/${objectName}/${uploadId}/${part}`,
			chunk
		);

		partETags[part] = result.eTag;
		part++;
	}

	// Finalize
	const finalize = await api.post<{ objectName: string }>('/api/files/upload/finalize', {
		ObjectName: objectName,
		UploadId: uploadId,
		partETags: partETags
	});

	return finalize.objectName;
}

export async function hashFile(file: File): Promise<string> {
	const buffer = await file.arrayBuffer();
	const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);

	return Array.from(new Uint8Array(hashBuffer))
		.map((b) => b.toString(16).padStart(2, '0'))
		.join('');
}
