import React from "react";

export default function Kbd({ children }: { children: React.ReactNode }) {
  return <kbd className="font-mono text-[10px] font-semibold">{children}</kbd>;
}
