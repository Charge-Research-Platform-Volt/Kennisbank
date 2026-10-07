import { describe, expect, it } from 'vitest';
import { buildSourceOrder, parseSegments, render, type ResolvedSource } from './render';

const U1 = '3f2b1c4e-9a7d-4b2e-8f1a-0c9d8e7f6a5b';
const U2 = 'a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d';

function sources(...entries: [string, ResolvedSource['type']][]): Map<string, ResolvedSource> {
	return new Map(entries.map(([id, type]) => [id, { id, name: `Source ${id.slice(0, 4)}`, type }]));
}

describe('render: citations', () => {
	it('shows an unresolved source as a numbered badge', () => {
		expect(render(`Solar is cheap[SRC:${U1}].`)).toBe(
			'<p>Solar is cheap.<span class="chat-cite-num">1</span></p>\n'
		);
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

		expect([...html.matchAll(/chat-cite-num">(\d+)</g)].map((m) => m[1])).toEqual(['1', '2', '1']);
	});

	it('expands comma-grouped markers into one citation per source', () => {
		expect(render(`Both agree[SRC:${U1}, ${U2}].`)).toBe(
			'<p>Both agree.<span class="chat-cite-num">1</span><span class="chat-cite-num">2</span></p>\n'
		);
	});

	it('treats a bare UUID as a citation', () => {
		expect(render(`As shown in ${U1}.`)).toBe(
			'<p>As shown in .<span class="chat-cite-num">1</span></p>\n'
		);
	});

	it('treats a bare UUID in parentheses as a citation', () => {
		expect(render(`Grid congestion is rising (${U1}).`)).toContain(
			'<span class="chat-cite-num">1</span>'
		);
	});

	it('leaves UUIDs inside link URLs alone', () => {
		const html = render(`[Report](/library?inspectorId=${U1}&inspectorType=resource)`);

		expect(html).toContain(`href="/library?inspectorId=${U1}&inspectorType=resource"`);
		expect(html).not.toContain('chat-cite-num');
	});

	it('swallows a stray colon glued to the marker', () => {
		expect(render(`As shown here:[SRC:${U1}]`)).toBe(
			'<p>As shown here<span class="chat-cite-num">1</span></p>\n'
		);
	});

	it('moves citations from before the period to after it', () => {
		expect(render(`Fact one[SRC:${U1}][SRC:${U2}]. Fact two.`)).toBe(
			'<p>Fact one.<span class="chat-cite-num">1</span><span class="chat-cite-num">2</span> Fact two.</p>\n'
		);
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
});
