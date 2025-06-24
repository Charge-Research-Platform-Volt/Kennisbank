import { marked } from "marked";
import { memo, useMemo } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { components } from "./_markdown/main";
import remarkMath from "remark-math";
import rehypeKatex from "rehype-katex";
import "katex/dist/katex.min.css";

/**
 * Parses a markdown string into an array of raw markdown blocks.x
 *
 * This function uses the marked lexer to tokenize the input markdown string
 * and then extracts the raw content of each token.
 *
 * @param markdown - The markdown string to parse into blocks
 * @returns An array of strings, where each string is a raw markdown block
 */
function parseMarkdownIntoBlocks(markdown: string): string[] {
  const tokens = marked.lexer(markdown);
  return tokens.map((token) => token.raw);
}

/**
 * MemoizedMarkdownBlock is a memoized component that renders markdown content.
 * It only re-renders when the content prop changes.
 *
 * @component
 * @param {Object} props - The component props
 * @param {string} props.content - The markdown content to be rendered
 * @returns {JSX.Element} A ReactMarkdown component that renders the provided content
 *
 * @example
 * ```tsx
 * <MemoizedMarkdownBlock content="# Hello, world!" />
 * ```
 */
const MemoizedMarkdownBlock = memo(
  ({ content }: { content: string }) => {
    return (
      <ReactMarkdown remarkPlugins={[remarkGfm, remarkMath]} rehypePlugins={[rehypeKatex]} components={components}>
        {content}
      </ReactMarkdown>
    );
  },
  (prevProps, nextProps) => {
    if (prevProps.content !== nextProps.content) return false;
    return true;
  },
);

MemoizedMarkdownBlock.displayName = "MemoizedMarkdownBlock";

/**
 * A memoized component that renders parsed markdown content.
 *
 * This component takes a markdown string, parses it into blocks using the
 * `parseMarkdownIntoBlocks` function, and renders each block as a
 * `MemoizedMarkdownBlock` component. The parsing is memoized to avoid
 * unnecessary re-computations when the component re-renders.
 *
 * @component
 * @param {Object} props - Component props
 * @param {string} props.content - The markdown content to render
 * @param {string} props.id - Unique identifier used to create keys for each markdown block
 * @returns {React.ReactNode} An array of MemoizedMarkdownBlock components
 */
export const MemoizedMarkdown = memo(({ content, id }: { content: string; id: string }) => {
  const blocks = useMemo(() => parseMarkdownIntoBlocks(content), [content]);

  return blocks.map((block, index) => <MemoizedMarkdownBlock content={block} key={`${id}-block_${index}`} />);
});

MemoizedMarkdown.displayName = "MemoizedMarkdown";
