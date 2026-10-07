import { marked, Renderer } from 'marked';
import markedKatex from 'marked-katex-extension';

marked.use(markedKatex({ throwOnError: false }));

export type AuthorRef = { id: string; name: string; fileType?: string };

export type ResolvedSource = {
	id: string;
	name: string;
	type: 'resource' | 'person' | 'organisation' | 'attachment';
	fileType?: string;
	sourceUrl?: string;
	authors?: AuthorRef[] | null;
};

export type Segment = { type: 'text' | 'ai'; content: string };

const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

const renderer = new Renderer();
renderer.link = ({ href, text }) => {
	// Normalize library links — strip any origin the LLM may have prepended
	let normalizedHref = href ?? '';
	try {
		const url = new URL(normalizedHref);
		if (url.pathname.startsWith('/library')) {
			normalizedHref = url.pathname + url.search;
		}
	} catch {
		/* already a relative URL */
	}

	if (/^\d+$/.test(text)) {
		const uuidMatch = normalizedHref.match(
			/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i
		);
		const uuid = uuidMatch?.[0] ?? '';
		return `<a href="${normalizedHref}" class="chat-cite-num" data-uuid="${uuid}" target="_blank" rel="noopener noreferrer">${text}</a>`;
	}
	if (normalizedHref.startsWith('/library')) {
		return `<a href="${normalizedHref}" class="chat-cite-source" title="${text}" target="_blank" rel="noopener noreferrer">${text}</a>`;
	}
	return `<a href="${normalizedHref}" class="chat-link" target="_blank" rel="noopener noreferrer">${text}</a>`;
};
marked.use({ renderer });

export function render(content: string, resolvedSources?: Map<string, ResolvedSource>): string {
	const uuidOrder = new Map<string, number>();
	let counter = 1;

	// Replace [SRC:uuid] / [ATTACH:uuid] markers (and comma-grouped variants), and bare UUIDs
	// the model occasionally emits without the required brackets — the resolved source's own
	// type decides the link target, not the marker kind, so this is robust either way. Also
	// swallow a stray colon glued directly in front of the marker with no space (another
	// formatting tic the model sometimes produces) rather than leaving it dangling in the text.
	// A bare UUID right after '=' or '/' is part of a URL (e.g. a markdown link), not a citation.
	let processed = content.replace(
		/:?(\[(?:SRC|ATTACH):[^\]]+\]|(?<![=/])[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/gi,
		(_fullMatch, marker: string) => {
			const uuids = [...marker.matchAll(uuidRe)].map((m) => m[0]);

			return uuids
				.map((uuid) => {
					if (!uuidOrder.has(uuid)) uuidOrder.set(uuid, counter++);
					const n = uuidOrder.get(uuid);
					const source = resolvedSources?.get(uuid);
					if (source) {
						const url =
							source.type === 'attachment'
								? `/api/files/${uuid}`
								: `/library?inspectorId=${uuid}&inspectorType=${source.type}`;
						return `[${n}](${url})`;
					}
					return `[[BADGE:${n}]]`;
				})
				.join('');
		}
	);

	// Citations often land immediately before the sentence's terminal period, wherever the
	// model happened to place the marker in its own text — move a run of one or more
	// consecutive citations to after the period instead, matching normal citation style.
	processed = processed.replace(/((?:\[\d+\]\([^)]+\)|\[\[BADGE:\d+\]\])+)\./g, '.$1');

	// Collapse a run of consecutive horizontal rules (blank lines in between) into a single one.
	processed = processed.replace(/(?:^---$\n*){2,}/gm, '---\n\n');

	let html = marked(processed) as string;

	// Replace [[BADGE:N]]
	html = html.replace(/\[\[BADGE:(\d+)\]\]/g, (_, n) => `<span class="chat-cite-num">${n}</span>`);

	return html;
}

/** Splits a message into plain text and [AI]...[/AI] general-knowledge blocks (an unclosed [AI] runs to the end). */
export function parseSegments(raw: string): Segment[] {
	const segments: Segment[] = [];
	const re = /\[AI\](.*?)\[\/AI\]/gs;

	let last = 0;

	for (const m of raw.matchAll(re)) {
		if (m.index! > last) segments.push({ type: 'text', content: raw.slice(last, m.index) });

		segments.push({ type: 'ai', content: m[1] });
		last = m.index! + m[0].length;
	}

	if (last < raw.length) {
		const remaining = raw.slice(last);
		const openTag = remaining.indexOf('[AI]');
		if (openTag !== -1) {
			if (openTag > 0) segments.push({ type: 'text', content: remaining.slice(0, openTag) });
			segments.push({ type: 'ai', content: remaining.slice(openTag + 4) });
		} else {
			segments.push({ type: 'text', content: remaining });
		}
	}

	return segments;
}

/** Numbers each cited source by first appearance, matching the numbers render() assigns. */
export function buildSourceOrder(content: string): Map<string, number> {
	const order = new Map<string, number>();
	let counter = 1;
	for (const m of content.matchAll(
		/\[(?:SRC|ATTACH):[^\]]+\]|(?<![=/])[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi
	)) {
		for (const u of m[0].matchAll(uuidRe)) {
			if (!order.has(u[0])) order.set(u[0], counter++);
		}
	}
	return order;
}
