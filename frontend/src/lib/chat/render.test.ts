import { describe, expect, it } from 'vitest';
import {
	buildSourceOrder,
	extractDocumentIds,
	parseSegments,
	render,
	sourceHref,
	type ResolvedSource
} from './render';

const U1 = '3f2b1c4e-9a7d-4b2e-8f1a-0c9d8e7f6a5b';
const U2 = 'a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d';

function sources(...entries: [string, ResolvedSource['type']][]): Map<string, ResolvedSource> {
	return new Map(entries.map(([id, type]) => [id, { id, name: `Source ${id.slice(0, 4)}`, type }]));
}

/** An unresolved citation: a plain number that still knows its source, for the hover popover */
const badge = (n: number, uuid: string) =>
	`<span class="chat-cite-num" data-uuid="${uuid}">${n}</span>`;

describe('render: citations', () => {
	it('shows an unresolved source as a numbered badge', () => {
		expect(render(`Solar is cheap[SRC:${U1}].`)).toBe(`<p>Solar is cheap.${badge(1, U1)}</p>\n`);
	});

	it('links a resolved library source to its inspector', () => {
		const html = render(`Solar is cheap[SRC:${U1}].`, sources([U1, 'resource']));

		expect(html).toContain(
			`<a href="/library?inspectorId=${U1}&inspectorType=resource" class="chat-cite-num" data-uuid="${U1}"`
		);
		expect(html).toContain('>1</a>');
	});

	it('links an attachment to the file download', () => {
		expect(render(`See the plan[ATTACH:${U1}].`, sources([U1, 'attachment']))).toContain(
			`href="/api/files/${U1}"`
		);
	});

	it('uses the resolved source type for the link, not the marker kind', () => {
		expect(render(`Text[ATTACH:${U1}].`, sources([U1, 'person']))).toContain(
			`/library?inspectorId=${U1}&inspectorType=person`
		);
	});

	it('numbers sources by first appearance and reuses numbers', () => {
		const html = render(`A[SRC:${U1}]. B[SRC:${U2}]. C[SRC:${U1}].`);

		expect([...html.matchAll(/chat-cite-num"[^>]*>(\d+)</g)].map((m) => m[1])).toEqual([
			'1',
			'2',
			'1'
		]);
	});

	it('expands comma-grouped markers into one citation per source', () => {
		expect(render(`Both agree[SRC:${U1}, ${U2}].`)).toBe(
			`<p>Both agree.${badge(1, U1)}${badge(2, U2)}</p>\n`
		);
	});

	it('treats a bare UUID as a citation', () => {
		expect(render(`As shown in ${U1}.`)).toBe(`<p>As shown in .${badge(1, U1)}</p>\n`);
	});

	it('treats a bare UUID in parentheses as a citation', () => {
		expect(render(`Grid congestion is rising (${U1}).`)).toContain(badge(1, U1));
	});

	it('leaves UUIDs inside link URLs alone', () => {
		const html = render(`[Report](/library?inspectorId=${U1}&inspectorType=resource)`);

		expect(html).toContain(`href="/library?inspectorId=${U1}&inspectorType=resource"`);
		expect(html).not.toContain('chat-cite-num');
	});

	it('swallows a stray colon glued to the marker', () => {
		expect(render(`As shown here:[SRC:${U1}]`)).toBe(`<p>As shown here${badge(1, U1)}</p>\n`);
	});

	it('moves citations from before the period to after it', () => {
		expect(render(`Fact one[SRC:${U1}][SRC:${U2}]. Fact two.`)).toBe(
			`<p>Fact one.${badge(1, U1)}${badge(2, U2)} Fact two.</p>\n`
		);
	});

	it('attaches a citation on a paragraph of its own to the text above it', () => {
		expect(render(`It is placeholder text at this time.\n\n[DOC:${U1}]`)).toBe(
			`<p>It is placeholder text at this time.${badge(1, U1)}</p>\n`
		);
	});

	it('puts citations after a quote behind its closing quotation mark', () => {
		const html = render(
			`The minutes now contain:\n\n> Test document\n> This is a test document\n\n[DOC:${U1}][DOC:${U2}]`
		);

		// The mark is written into the text, so the badges come after it; quote-cited turns off the CSS one
		expect(html).toContain(
			`<p class="quote-cited">Test document\nThis is a test document<span class="quote-close">”</span>${badge(1, U1)}${badge(2, U2)}</p>\n</blockquote>`
		);
		expect(html).toContain('<p>The minutes now contain:</p>');
	});

	it('handles a citation written at the end of the quote itself, and a resolved one', () => {
		const html = render(`> Quoted text [SRC:${U1}]`, sources([U1, 'resource']));

		expect(html).toMatch(
			/<p class="quote-cited">Quoted text<span class="quote-close">”<\/span><a [^>]*class="chat-cite-num"[^>]*>1<\/a><\/p>/
		);
	});

	it('only touches the last paragraph of a quote, and quotes without citations not at all', () => {
		const html = render(`> First\n>\n> Second\n\n[SRC:${U1}]\n\n> Plain quote`);

		expect(html).toContain('<p>First</p>');
		expect(html).toContain(
			`<p class="quote-cited">Second<span class="quote-close">”</span>${badge(1, U1)}</p>`
		);
		expect(html).toContain('<p>Plain quote</p>');
		expect(html.match(/quote-cited/g)).toHaveLength(1);
	});

	it('attaches citation lines below each other in order', () => {
		expect(render(`Text.\n\n[SRC:${U1}]\n\n[SRC:${U2}]`)).toBe(
			`<p>Text.${badge(1, U1)}${badge(2, U2)}</p>\n`
		);
	});

	it('leaves a citation alone when there is nothing to attach it to', () => {
		expect(render(`[SRC:${U1}]`)).toBe(`<p>${badge(1, U1)}</p>\n`);
	});

	it('never appends a citation to a code fence, table row or horizontal rule', () => {
		expect(render('```\ncode\n```\n\n[SRC:' + U1 + ']')).toContain(
			'<pre><code>code\n</code></pre>'
		);
		expect(render(`| a | b |\n| - | - |\n| 1 | 2 |\n\n[SRC:${U1}]`)).toContain('<td>2</td>');
		expect(render(`Text\n\n***\n\n[SRC:${U1}]`)).toContain('<hr>');
	});

	it('leaves text without citations as plain markdown', () => {
		expect(render('**Bold** and [1] stay as they are.')).toBe(
			'<p><strong>Bold</strong> and [1] stay as they are.</p>\n'
		);
	});
});

describe('render: markdown', () => {
	it('collapses consecutive horizontal rules into one', () => {
		expect(render('Above\n\n---\n\n---\n\n---\n\nBelow').match(/<hr>/g)).toHaveLength(1);
	});

	it('normalises library links to a relative path', () => {
		const html = render(
			`[Report](https://kennisbank.example.org/library?inspectorId=${U1}&inspectorType=resource)`
		);

		expect(html).toContain(
			`<a href="/library?inspectorId=${U1}&inspectorType=resource" class="chat-cite-source" title="Report"`
		);
	});

	it('opens external links in a new tab', () => {
		expect(render('[TNO](https://www.tno.nl)')).toContain(
			'<a href="https://www.tno.nl" class="chat-link" target="_blank" rel="noopener noreferrer">TNO</a>'
		);
	});

	it('renders inline and display math with KaTeX', () => {
		const html = render('Inline $x^2$ and display:\n\n$$E=mc^2$$');

		expect(html).toContain('class="katex"');
		expect(html).toContain('class="katex-display"');
	});
});

describe('parseSegments', () => {
	it('returns a single text segment when there is no [AI] block', () => {
		expect(parseSegments('Only library content.')).toEqual([
			{ type: 'text', content: 'Only library content.' }
		]);
	});

	it('splits text and [AI] blocks in order', () => {
		expect(parseSegments('Intro. [AI]General context.[/AI] Back to sources.')).toEqual([
			{ type: 'text', content: 'Intro. ' },
			{ type: 'ai', content: 'General context.' },
			{ type: 'text', content: ' Back to sources.' }
		]);
	});

	it('handles multiple and multi-line [AI] blocks', () => {
		expect(parseSegments('[AI]One\n- a\n- b[/AI]Middle[AI]Two[/AI]')).toEqual([
			{ type: 'ai', content: 'One\n- a\n- b' },
			{ type: 'text', content: 'Middle' },
			{ type: 'ai', content: 'Two' }
		]);
	});

	it('treats an unclosed [AI] block as running to the end (while streaming)', () => {
		expect(parseSegments('Intro. [AI]Still being writ')).toEqual([
			{ type: 'text', content: 'Intro. ' },
			{ type: 'ai', content: 'Still being writ' }
		]);
	});
});

describe('buildSourceOrder', () => {
	it('numbers sources by first appearance, matching render()', () => {
		const order = buildSourceOrder(
			`A[SRC:${U2}]. B[ATTACH:${U1}]. C[SRC:${U2}]. D[SRC:${U1}, ${U2}].`
		);

		expect([...order]).toEqual([
			[U2, 1],
			[U1, 2]
		]);
	});

	it('includes bare UUIDs and ignores text without citations', () => {
		expect([...buildSourceOrder(`See ${U1}.`)]).toEqual([[U1, 1]]);
		expect(buildSourceOrder('No citations [1] here.').size).toBe(0);
		expect(buildSourceOrder(`[Report](/library?inspectorId=${U1})`).size).toBe(0);
	});

	it('counts project document markers like any other citation', () => {
		expect([...buildSourceOrder(`A[DOC:${U2}]. B[SRC:${U1}].`)]).toEqual([
			[U2, 1],
			[U1, 2]
		]);
	});
});

describe('project document citations', () => {
	const PROJECT = '0b1c2d3e-4f50-4a61-8b72-9c8d7e6f5a4b';
	const document = (id: string): Map<string, ResolvedSource> =>
		new Map([[id, { id, name: 'Minutes', type: 'document', projectId: PROJECT }]]);

	it('links a document to its viewer, in the same tab next to the chat', () => {
		const html = render(`Decided in June[DOC:${U1}].`, document(U1));

		expect(html).toContain(`href="/projects/${PROJECT}/documents/${U1}"`);
		expect(html).not.toContain('target="_blank"');
	});

	it('points the badge at the document, not at the project id that comes first in the URL', () => {
		expect(render(`Decided in June[DOC:${U1}].`, document(U1))).toContain(`data-uuid="${U1}"`);
	});

	it('shows a document the user can no longer open as a plain badge that still knows its id', () => {
		// The id lets the popover explain the source is deleted or no longer accessible
		expect(render(`Decided in June[DOC:${U1}].`)).toContain(badge(1, U1));
	});

	it('still opens library sources in a new tab', () => {
		expect(render(`Solar[SRC:${U1}].`, sources([U1, 'resource']))).toContain('target="_blank"');
	});

	it('sourceHref picks the target per source type', () => {
		expect(sourceHref({ id: U1, name: 'Plan', type: 'document', projectId: PROJECT })).toBe(
			`/projects/${PROJECT}/documents/${U1}`
		);
		expect(sourceHref({ id: U1, name: 'Plan', type: 'attachment' })).toBe(`/api/files/${U1}`);
		expect(sourceHref({ id: U1, name: 'Plan', type: 'organisation' })).toBe(
			`/library?inspectorId=${U1}&inspectorType=organisation`
		);
	});

	it('extractDocumentIds only returns ids cited as documents, once each', () => {
		expect(
			extractDocumentIds(`A[DOC:${U1}]. B[SRC:${U2}]. C[DOC:${U1}]. D ${U2}. E[DOC:${U2}, ${U1}].`)
		).toEqual([U1, U2]);
		expect(extractDocumentIds(`Only[SRC:${U1}] and ${U2}.`)).toEqual([]);
	});
});
