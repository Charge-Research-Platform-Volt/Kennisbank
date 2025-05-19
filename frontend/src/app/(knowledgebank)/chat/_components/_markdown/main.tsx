import type { ExtraProps } from "react-markdown";
import type { ComponentProps, ElementType } from "react";
import { Heading } from "./heading";
import { Paragraph } from "./paragraph";
import { List } from "./list";
import { HorizontalRule } from "./horizontal-rule";
import { CodeBlock } from "./code-block";
import { CodeInline } from "./code-inline";
import { Link } from "./link";
import { TableRow } from "./table-row";
import { TableCell } from "./table-cell";
import { Table } from "./table";
import { BlockQuote } from "./blockquote";
import { Strong } from "./strong";
import { Emphasis } from "./emphasis";
import { Delete } from "./delete";

// This disables the rule for unused vars
/* eslint-disable @typescript-eslint/no-unused-vars */
// Do not pass `node` to the components, otherwise it will cause a node="[object Object]" in the DOM

type Components = {
  [Key in Extract<ElementType, string>]?: ElementType<ComponentProps<Key> & ExtraProps>;
};

interface CodeProps extends ComponentProps<"code">, ExtraProps {
  inline?: boolean;
  className?: string;
}

export const components: Components = {
  // Headings
  h1: ({ children, node, ...props }) => (
    <Heading as="h1" {...props}>
      {children}
    </Heading>
  ),
  h2: ({ children, node, ...props }) => (
    <Heading as="h2" {...props}>
      {children}
    </Heading>
  ),
  h3: ({ children, node, ...props }) => (
    <Heading as="h3" {...props}>
      {children}
    </Heading>
  ),
  h4: ({ children, node, ...props }) => (
    <Heading as="h4" {...props}>
      {children}
    </Heading>
  ),
  h5: ({ children, node, ...props }) => (
    <Heading as="h5" {...props}>
      {children}
    </Heading>
  ),

  h6: ({ children, node, ...props }) => (
    <Heading as="h6" {...props}>
      {children}
    </Heading>
  ),

  // Paragraph
  p: ({ children, node, ...props }) => <Paragraph {...props}>{children}</Paragraph>,

  //   List
  ul: ({ children, node, ...props }) => (
    <List ordered={false} {...props}>
      {children}
    </List>
  ),
  ol: ({ children, node, ...props }) => (
    <List ordered={true} {...props}>
      {children}
    </List>
  ),

  // HorizontalRule
  hr: HorizontalRule,

  // CodeBlock & CodeInline
  code: ({ inline, node, className, children, ...props }: CodeProps) => {
    const match = /language-(\w+)/.exec(className || "");
    return !inline && match ? <CodeBlock language={match[1]} value={String(children).replace(/\n$/, "")} {...props} /> : <CodeInline {...props}>{children}</CodeInline>;
  },

  // Link
  a: ({ children, href = "#", ...props }) => (
    <Link href={href} {...props}>
      {children}
    </Link>
  ),

  // Table
  table: ({ children, node, ...props }) => <Table {...props}>{children}</Table>,
  tr: ({ children, node, ...props }) => <TableRow {...props}>{children}</TableRow>,
  td: ({ children, node, ...props }) => (
    <TableCell isHeader={false} {...props}>
      {children}
    </TableCell>
  ),
  th: ({ children, node, ...props }) => (
    <TableCell isHeader={true} {...props}>
      {children}
    </TableCell>
  ),

  // BlockQuote
  blockquote: ({ children, node, ...props }) => <BlockQuote children={children} {...props} />,

  // Strong
  strong: ({ children, node, ...props }) => <Strong {...props}>{children}</Strong>,

  // Emphasis
  em: ({ children, node, ...props }) => <Emphasis {...props}>{children}</Emphasis>,

  // Strikethrough
  del: ({ children, node, ...props }) => <Delete {...props}>{children}</Delete>,
};
