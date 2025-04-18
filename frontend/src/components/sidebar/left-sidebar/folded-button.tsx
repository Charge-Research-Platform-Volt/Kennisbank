"use client";

import React from "react";

export default function FoldedButton({ action, className = "", icon } : { action: () => void, className?: string, icon: React.JSX.Element }) 
  {
    return (
        <button
            type="button"
            data-testid="sidebar_hide"
            onClick={action}
            className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 ${className}`}
            style={{ cursor: "pointer" }}
        >
            {icon}
        </button>
    )
}
