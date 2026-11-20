import { marked, Renderer } from 'marked';
import markedKatex from 'marked-katex-extension';

marked.use(markedKatex({ throwOnError: false }));

export type AuthorRef = { id: string; name: string; fileType?: string };

export type ResolvedSource = {
	id: string;
	name: string;
	type: 'resource' | 'person' | 'organisation' | 'attachment' | 'document';
	fileType?: string;
	sourceUrl?: string;
	authors?: AuthorRef[] | null;
	/** A project document's root project, which the viewer URL needs */
	projectId?: string;
};

export type Segment = { type: 'text' | 'ai'; content: string };

const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

/** Where a cited source opens: the file, the project document viewer, or the library inspector */
export function sourceHref(source: ResolvedSource): string {
	switch (source.type) {
		case 'attachment':
			return `/api/files/${source.id}`;
		case 'document':
			return `/projects/${source.projectId}/documents/${source.id}`;
		default:
			return `/library?inspectorId=${source.id}&inspectorType=${source.type}`;
	}
}

/** A project document opens in the same tab: the viewer sits next to the project chat, which stays open */
export const opensInPlace = (href: string) => href.startsWith('/projects/');

/** The ids cited as project documents, so they're looked up as documents and not as library items */
export function extractDocumentIds(content: string): string[] {
	const ids = [...content.matchAll(/\[DOC:([^\]]+)\]/gi)].flatMap((m) =>
		[...m[1].matchAll(uuidRe)].map((u) => u[0])
	);
	return [...new Set(ids)];
}

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

	const target = opensInPlace(normalizedHref) ? '' : ' target="_blank" rel="noopener noreferrer"';

	if (/^\d+$/.test(text)) {
		// A viewer link starts with the project id, so the cited source is the last UUID in the URL
		const uuid = normalizedHref.match(uuidRe)?.at(-1) ?? '';
		return `<a href="${normalizedHref}" class="chat-cite-num" data-uuid="${uuid}"${target}>${text}</a>`;
	}

	if (normalizedHref.startsWith('/library')) {
		return `<a href="${normalizedHref}" class="chat-cite-source" title="${text}" target="_blank" rel="noopener noreferrer">${text}</a>`;
	}
	return `<a href="${normalizedHref}" class="chat-link" target="_blank" rel="noopener noreferrer">${text}</a>`;
};
marked.use({ renderer });

// One or more citations once the markers are replaced: links [n](url) or badges [[BADGE:n:uuid]]
const citationOnlyLine = /^\s*((?:\[\d+\]\([^)]+\)|\[\[BADGE:\d+:[0-9a-f-]+\]\])+)\s*$/i;
// Appending a citation would break these: code fences, table rows and horizontal rules
const isStructural = (line: string) => /^\s*(```|~~~|\|)|^\s*([-*_])(\s*\2){2,}\s*$/.test(line);

function previousTextLine(lines: string[], from: number): number | null {
	for (let i = from - 1; i >= 0; i--) if (lines[i].trim() !== '') return i;
	return null;
}

/**
 * Attaches citations the model put on a line of their own to the text above them. After a quote that is the quote's
 * last line; citeQuotes() then moves them behind its closing quotation mark.
 */
function attachLooseCitations(text: string): string {
	const lines = text.split('\n');

	for (let i = 0; i < lines.length; i++) {
		const citations = lines[i].match(citationOnlyLine)?.[1];
		if (!citations) continue;

		const target = previousTextLine(lines, i);
		// Nothing to attach to (the message is only citations): leave it where it is
		if (target === null || isStructural(lines[target])) continue;

		lines[target] = lines[target].trimEnd() + citations;
		lines[i] = '';
	}

	return lines.join('\n');
}

// A run of rendered citations: linked numbers and unavailable badges
const citationHtml = String.raw`(?:<a [^>]*class="chat-cite-num"[^>]*>\d+<\/a>|<span class="chat-cite-num"[^>]*>\d+<\/span>)+`;
const citedQuoteEnd = new RegExp(
	String.raw`<p>((?:(?!<p>)[\s\S])*?)(${citationHtml})<\/p>(\s*<\/blockquote>)`,
	'g'
);

/**
 * The chat's prose styling draws the quotation marks around a quote with CSS, so citations at the end of a quote
 * would sit inside the closing mark. Writes that mark into the text instead, before the citations; the quote-cited
 * class turns the CSS one off.
 */
function citeQuotes(html: string): string {
	return html.replace(
		citedQuoteEnd,
		(_, text: string, citations: string, end: string) =>
			`<p class="quote-cited">${text.trimEnd()}<span class="quote-close">”</span>${citations}</p>${end}`
	);
}

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
		/:?(\[(?:SRC|ATTACH|DOC):[^\]]+\]|(?<![=/])[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/gi,
		(_fullMatch, marker: string) => {
			const uuids = [...marker.matchAll(uuidRe)].map((m) => m[0]);

			return uuids
				.map((uuid) => {
					if (!uuidOrder.has(uuid)) uuidOrder.set(uuid, counter++);
					const n = uuidOrder.get(uuid);
					const source = resolvedSources?.get(uuid);
					if (source) return `[${n}](${sourceHref(source)})`;

					return `[[BADGE:${n}:${uuid}]]`;
				})
				.join('');
		}
	);

	// Citations often land immediately before the sentence's terminal period, wherever the
	// model happened to place the marker in its own text — move a run of one or more
	// consecutive citations to after the period instead, matching normal citation style.
	processed = processed.replace(/((?:\[\d+\]\([^)]+\)|\[\[BADGE:\d+:[0-9a-f-]+\]\])+)\./gi, '.$1');

	processed = attachLooseCitations(processed);

	// Collapse a run of consecutive horizontal rules (blank lines in between) into a single one.
	processed = processed.replace(/(?:^---$\n*){2,}/gm, '---\n\n');

	let html = marked(processed) as string;

	// An unavailable source keeps its id, so hovering the badge can still explain why it has no link
	html = html.replace(
		/\[\[BADGE:(\d+):([0-9a-f-]+)\]\]/gi,
		(_, n, uuid) => `<span class="chat-cite-num" data-uuid="${uuid}">${n}</span>`
	);

	return citeQuotes(html);
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
		/\[(?:SRC|ATTACH|DOC):[^\]]+\]|(?<![=/])[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi
	)) {
		for (const u of m[0].matchAll(uuidRe)) {
			if (!order.has(u[0])) order.set(u[0], counter++);
		}
	}
	return order;
}
