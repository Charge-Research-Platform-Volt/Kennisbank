import React from "react";

export default function Divider({ name }: { name?: string }) {
  return (
    <div className="flex w-full flex-row items-center gap-2">
      {name && <div className="text-xs font-medium text-gray-400 uppercase">{name}</div>}
      <div className="h-px w-full bg-gray-300"></div>
    </div>
  );
}
