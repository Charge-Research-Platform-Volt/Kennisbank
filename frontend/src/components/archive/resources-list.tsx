"use client";

import { useArchive } from "@/context/archive-provider";
import { useSidebar } from "@/context/sidebar-provider";
import React from "react";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";

export default function ResourcesList() {
  // Get archive data and mode from context
  const { rowData, mode } = useArchive();

  // Get sidebar controls from context
  const { openRightSidebar } = useSidebar();

  // Limit the number of chunks displayed initially
  const LIMIT_CHUNKS = 2;

  return (
    <Accordion type="multiple">
      {rowData.map((resource) => (
        <AccordionItem value={resource.id} className="p-4" key={resource.id}>
          {/* Main resource container - clickable to open sidebar */}
          <div className="cursor-pointer space-y-1" onClick={() => openRightSidebar(resource.id, resource.type)}>
            {/* Resource title */}
            <h3 className="text-lg font-semibold">{resource.name}</h3>

            {/* Publication date */}
            <p className="text-sm text-gray-500"> {new Date(resource.publicationDate).toLocaleDateString()}</p>

            {/* Show description only in general results mode */}
            {mode === "general-results" && <p className="text-xs text-gray-500">{resource.description}</p>}

            {/* Render chunks if they exist */}
            {resource.chunks && resource.chunks.length > 0 && (
              <div className="mt-2 space-y-2">
                {/* Display limited number of chunks initially */}
                {resource.chunks.slice(0, LIMIT_CHUNKS).map((chunk, index) => (
                  <p key={index} className="text-sm text-balance text-gray-600">
                    {chunk}
                  </p>
                ))}

                {/* Show accordion trigger for additional chunks if available */}
                {resource.chunks.length > LIMIT_CHUNKS && (
                  <>
                    {/* Accordion trigger to expand remaining chunks */}
                    <AccordionTrigger onClick={(e) => e.stopPropagation()} className="my-2 justify-start gap-1 p-0 text-xs text-gray-500 underline">
                      +{resource.chunks.length - LIMIT_CHUNKS} more
                    </AccordionTrigger>

                    {/* Collapsible content showing remaining chunks */}
                    <AccordionContent className="space-y-2 text-sm text-balance text-gray-600">
                      {resource.chunks.slice(LIMIT_CHUNKS).map((chunk, index) => (
                        <p key={index}>{chunk}</p>
                      ))}
                    </AccordionContent>
                  </>
                )}
              </div>
            )}
          </div>
        </AccordionItem>
      ))}
    </Accordion>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
