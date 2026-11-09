import { describe, expect, it } from 'vitest';
import { Globe, Lock, Users } from '@lucide/svelte';
import {
	downloadUrl,
	formatFileSize,
	getVisibilityIcon,
	isProcessing,
	previewUrl,
	titleFromFileName
} from './project-documents';
import type { EmbeddingStatus } from './types/resource';
import type { ProjectDocumentVersion } from './types/project-document';

function version(
	previewStatus: EmbeddingStatus,
	embeddingStatus: EmbeddingStatus
): ProjectDocumentVersion {
	return {
		id: 'v1',
		versionNumber: 1,
		extension: 'docx',
		originalFileName: 'plan',
		sizeBytes: null,
		note: null,
		uploadedBy: 'u1',
		uploadedByName: 'Test User',
		uploadedOn: '2026-11-09T12:00:00Z',
		previewStatus,
		embeddingStatus
	};
}

describe('isProcessing', () => {
	it.each([
		['Pending', 'Pending', true],
		['Processing', 'Completed', true], // the preview is still converting
		['Completed', 'Processing', true], // the preview is ready, embedding isn't
		['Failed', 'Pending', true],
		['Completed', 'Completed', false],
		['Completed', 'Failed', false], // finished, just not successfully: nothing to wait for
		['Failed', 'Failed', false]
	] as [EmbeddingStatus, EmbeddingStatus, boolean][])(
		'preview %s, embedding %s → %s',
		(preview, embedding, expected) => {
			expect(isProcessing(version(preview, embedding))).toBe(expected);
		}
	);
});

describe('titleFromFileName', () => {
	it.each([
		['Minutes 4 Nov.docx', 'Minutes 4 Nov'],
		['report.final.pdf', 'report.final'], // only the last extension goes
		['  spaced .pdf', 'spaced'],
		['no-extension', 'no-extension'],
		['.hidden', '.hidden'] // a leading dot isn't an extension
	])('%j → %j', (fileName, expected) => {
		expect(titleFromFileName(fileName)).toBe(expected);
	});
});

describe('formatFileSize', () => {
	it.each([
		[null, '-'],
		[0, '0 B'],
		[1023, '1023 B'],
		[1024, '1 KB'],
		[512_000, '500 KB'],
		[1_048_576, '1.0 MB'],
		[15_938_355, '15.2 MB']
	])('%j → %j', (bytes, expected) => {
		expect(formatFileSize(bytes)).toBe(expected);
	});
});

describe('getVisibilityIcon', () => {
	it('shows how far a document is shared', () => {
		expect(getVisibilityIcon('Restricted')).toBe(Lock);
		expect(getVisibilityIcon('ProjectMembers')).toBe(Users);
		expect(getVisibilityIcon('Everyone')).toBe(Globe);
	});
});

describe('document urls', () => {
	it('go through the project the document is opened from', () => {
		expect(previewUrl('folder', 'doc', 'v2')).toBe(
			'/api/projects/folder/documents/doc/versions/v2/preview'
		);
		expect(downloadUrl('root', 'doc', 'v1')).toBe(
			'/api/projects/root/documents/doc/versions/v1/download'
		);
	});
});
