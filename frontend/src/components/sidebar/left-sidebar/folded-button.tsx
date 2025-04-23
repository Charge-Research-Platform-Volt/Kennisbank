"use client";

import React from "react";

/**
 * 
 * @param action - Onclick action for the button
 * @param className - Styling from root
 * @param icon - Icon to be displayed on this button
 * @param testid - Test-id for use in testing
 * @returns A button styled from root that's been given an action and an icon. Used for the folded left sidebar.
 */
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
