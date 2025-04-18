"use client";

import React from "react";

export default function FoldedButton({ action, className = "", icon, testid = undefined } : { action: () => void, className?: string, icon: React.JSX.Element, testid?: string }) 
  {
    return (
        <button
            type="button"
            onClick={action}
            {...testid == undefined ? {} : { "data-testid": testid }}
            className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 ${className}`}
            style={{ cursor: "pointer" }}
        >
            {icon}
        </button>
    )
}
