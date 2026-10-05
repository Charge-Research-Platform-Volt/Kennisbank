import { Marked, Renderer } from 'marked';

const renderer = new Renderer();
renderer.link = ({ href, text }) => {
	const isExternal = /^https?:\/\//i.test(href ?? '');
	const target = isExternal ? ' target="_blank" rel="noopener noreferrer"' : '';
	return `<a href="${href}" class="text-primary underline underline-offset-4"${target}>${text}</a>`;
};

const changelogMarked = new Marked({ renderer });

export function renderChangelog(body: string): string {
	return changelogMarked.parse(body, { async: false });
}
