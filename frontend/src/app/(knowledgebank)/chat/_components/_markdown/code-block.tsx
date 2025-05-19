"use client";

import React from "react";
import { PrismLight as SyntaxHighlighter } from "react-syntax-highlighter";
import python from "react-syntax-highlighter/dist/esm/languages/prism/python";
import { oneLight } from "react-syntax-highlighter/dist/esm/styles/prism";
import CopyButton from "../copy-button";

// Languages
import bash from "react-syntax-highlighter/dist/esm/languages/prism/bash";
import javascript from "react-syntax-highlighter/dist/esm/languages/prism/javascript";
import typescript from "react-syntax-highlighter/dist/esm/languages/prism/typescript";
import jsx from "react-syntax-highlighter/dist/esm/languages/prism/jsx";
import tsx from "react-syntax-highlighter/dist/esm/languages/prism/tsx";
import css from "react-syntax-highlighter/dist/esm/languages/prism/css";
import json from "react-syntax-highlighter/dist/esm/languages/prism/json";
import sql from "react-syntax-highlighter/dist/esm/languages/prism/sql";
import yaml from "react-syntax-highlighter/dist/esm/languages/prism/yaml";
import csharp from "react-syntax-highlighter/dist/esm/languages/prism/csharp";
import go from "react-syntax-highlighter/dist/esm/languages/prism/go";
import html from "react-syntax-highlighter/dist/esm/languages/prism/markup";
import php from "react-syntax-highlighter/dist/esm/languages/prism/php";
import ruby from "react-syntax-highlighter/dist/esm/languages/prism/ruby";
import swift from "react-syntax-highlighter/dist/esm/languages/prism/swift";
import latex from "react-syntax-highlighter/dist/esm/languages/prism/latex";

// Register the languages
SyntaxHighlighter.registerLanguage("bash", bash);
SyntaxHighlighter.registerLanguage("javascript", javascript);
SyntaxHighlighter.registerLanguage("js", javascript);
SyntaxHighlighter.registerLanguage("typescript", typescript);
SyntaxHighlighter.registerLanguage("ts", typescript);
SyntaxHighlighter.registerLanguage("jsx", jsx);
SyntaxHighlighter.registerLanguage("tsx", tsx);
SyntaxHighlighter.registerLanguage("css", css);
SyntaxHighlighter.registerLanguage("json", json);
SyntaxHighlighter.registerLanguage("python", python);
SyntaxHighlighter.registerLanguage("py", python);
SyntaxHighlighter.registerLanguage("sql", sql);
SyntaxHighlighter.registerLanguage("yaml", yaml);
SyntaxHighlighter.registerLanguage("yml", yaml);
SyntaxHighlighter.registerLanguage("csharp", csharp);
SyntaxHighlighter.registerLanguage("go", go);
SyntaxHighlighter.registerLanguage("html", html);
SyntaxHighlighter.registerLanguage("php", php);
SyntaxHighlighter.registerLanguage("ruby", ruby);
SyntaxHighlighter.registerLanguage("swift", swift);
SyntaxHighlighter.registerLanguage("latex", latex);

interface CodeBlockProps {
  language: string;
  value: string;
}

export function CodeBlock({ language, value, ...props }: CodeBlockProps) {
  return (
    <div className="bg-sidebar relative my-4 rounded-lg border">
      <div className="flex items-center justify-between border-b py-1 pr-1 pl-3">
        <div className="text-muted-foreground text-xs font-medium uppercase">{language}</div>
        <CopyButton value={value} />
      </div>

      <SyntaxHighlighter
        language={language}
        style={oneLight}
        customStyle={{
          margin: 0,
          overflowX: "auto",
          borderRadius: "0 0 0.5rem 0.5rem",
          paddingBottom: "1rem",
          backgroundColor: "transparent",
          fontSize: "0.875rem",
        }}
        codeTagProps={{
          style: {
            background: "transparent",
          },
        }}
        {...props}
      >
        {value}
      </SyntaxHighlighter>
    </div>
  );
}
