import { api } from './api';

export type UploadPurpose = 'Library' | 'ProjectDocument';

export async function uploadFile(file: File, purpose: UploadPurpose = 'Library'): Promise<string> {
	const CHUNK_SIZE = 5242880; // 5 MB

	// Init
	const init = await api.post<{ objectName: string; uploadId: string }>('/api/files/upload/init', {
		FileName: file.name,
		FileSize: file.size,
		Purpose: purpose
	});
	const { objectName, uploadId } = init;

	// Upload chunks
	const partETags: Record<number, string> = {};
	let part = 1;

	for (let offset = 0; offset < file.size; offset += CHUNK_SIZE) {
		const chunk = file.slice(offset, offset + CHUNK_SIZE);
		const result = await api.postBinary<{ eTag: string }>(
			`/api/files/upload/part/${objectName}/${uploadId}/${part}?purpose=${purpose}`,
			chunk
		);

		partETags[part] = result.eTag;
		part++;
	}

	// Finalize
	await api.post('/api/files/upload/finalize', {
		ObjectName: objectName,
		UploadId: uploadId,
		Purpose: purpose,
		partETags: partETags
	});

	return objectName;
}

export async function hashFile(file: File): Promise<string> {
	const buffer = await file.arrayBuffer();
	const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);

	return Array.from(new Uint8Array(hashBuffer))
		.map((b) => b.toString(16).padStart(2, '0'))
		.join('');
}
