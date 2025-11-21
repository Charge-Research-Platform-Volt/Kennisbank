"use client";

import { useArchive } from "@/context/archive-provider";
import { useArchiveSidebar } from "@/context/archive-sidebar-provider";
import React from "react";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import GetFileIcon from "../getFileIcon";
import PersonIcon from "@/icons/person-icon";
import OrganisationIcon from "@/icons/organisation-icon";

export default function ResourcesList() {
  // Get archive data and mode from context
  const { rowData, mode } = useArchive();

  // Get sidebar controls from context
  const { openArchiveSidebar } = useArchiveSidebar();

  // Limit the number of chunks displayed initially
  const LIMIT_CHUNKS = 2;

  // Helper function to format date based on precision
  const formatDate = (dateString: string, precision: 'Year' | 'Month' | 'Day' | 0 | 1 | 2) => {
    if (!dateString) return 'Unknown';

    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = date.getMonth();

    // Normalize precision
    const normalizedPrecision = precision === 0 || precision === 'Year' ? 'Year'
      : precision === 1 || precision === 'Month' ? 'Month'
      : 'Day';

    if (normalizedPrecision === 'Year') {
      return year.toString();
    } else if (normalizedPrecision === 'Month') {
      const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
                        'July', 'August', 'September', 'October', 'November', 'December'];
      return `${monthNames[month]} ${year}`;
    } else {
      return date.toLocaleDateString();
    }
  };

  return (
    <Accordion type="multiple">
      {rowData.map((resource) => (
        <AccordionItem value={resource.id} className="p-4" key={resource.id}>
          {/* Main resource container - clickable to open sidebar */}
          <div className="cursor-pointer space-y-1" onClick={() => openArchiveSidebar(resource.id, resource.type)}>
            {/* Resource title */}
            <div className="flex justify-start gap-2 items-center">
              {resource.type === 'resource' && <GetFileIcon fileType={resource.fileType} className="w-5 h-5" />}
              {resource.type === 'person' && <PersonIcon className="w-5 h-5" />}
              {resource.type === 'organisation' && <OrganisationIcon className="w-5 h-5" />}
              <h3 className="text-lg font-semibold">{resource.name}</h3>
            </div>

            {/* Publication date */}
            <p className="text-sm text-gray-500">{formatDate(resource.publicationDate, resource.publicationDatePrecision)}</p>

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
