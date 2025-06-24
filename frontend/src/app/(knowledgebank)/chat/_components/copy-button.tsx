"use client";

import { Button } from "@/components/ui/button";
import { Check, Copy } from "lucide-react";
import React from "react";

export default function CopyButton({ value }: { value: string }) {
  const [copied, setCopied] = React.useState(false);

  const copyToClipboard = () => {
    navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Button variant="ghost" size="icon" className="h-7 w-7 opacity-70 hover:opacity-100 [&_svg:not([class*='size-'])]:size-4" onClick={copyToClipboard} disabled={copied}>
      {copied ? <Check className="h-2 w-2" /> : <Copy className="h-2 w-2" />}
      <span className="sr-only">{copied ? "Copied!" : "Copy code"}</span>
    </Button>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


