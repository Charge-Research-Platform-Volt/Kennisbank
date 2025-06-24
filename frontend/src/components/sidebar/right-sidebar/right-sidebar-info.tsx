"use client";

import React from "react";
import { MetadataTypeEnum } from "@/context/sidebar-provider";
import { ResourceContent } from "./ResourceContent";
import { PersonContent } from "./PersonContent";
import { OrganisationContent } from "./OrganisationContent";
import { Button } from "@/components/ui/button";
import HideMenu from "@/icons/menu/hide-menu";
import { useSidebar } from "@/context/sidebar-provider";
import { openFile } from "@/actions/openFileActions";

export default function RightSidebarInfo() {
  const { currentType, currentId, navigateBack, navigateForward, setRightSidebarOpen, isEmptyPrevs, isEmptyNexts } = useSidebar();

  const handleOpenClick = () => {
    const str = openFile(currentId);

    str
      .then((response) => {
        if (response != undefined) {
          window.open(response, "_blank");
        }
      })
      .catch();
  };

  return (
    <>
      {/* Navigation buttons */}
      <div className="flex w-full justify-between space-x-2 p-2">
        {/* Hide menu button */}
        <Button
          data-testid="sidebar_hide"
          variant="outline"
          size="icon"
          className="h-10 w-10"
          onClick={() => {
            setRightSidebarOpen(false);
          }}
        >
          <HideMenu flip={true} />
        </Button>

        <Button variant="outline" onClick={handleOpenClick} disabled={currentType === MetadataTypeEnum.RESOURCE ? false : true} className="h-10 flex-1">
          Download
        </Button>

        <div className="flex justify-center space-x-2">
          <Button variant="outline" onClick={navigateBack} className="h-10 w-[8rem]" disabled={isEmptyPrevs()}>
            Previous
          </Button>
          <Button variant="outline" onClick={navigateForward} className="h-10 w-[8rem]" disabled={isEmptyNexts()}>
            Next
          </Button>
        </div>
      </div>

      {/* Content area */}
      <div className="h-full w-full flex-1 overflow-y-auto p-4 pb-25">
        {(() => {
          switch (currentType) {
            case MetadataTypeEnum.RESOURCE:
              return <ResourceContent />;
            case MetadataTypeEnum.PERSON:
              return <PersonContent />;
            case MetadataTypeEnum.ORGANISATION:
              return <OrganisationContent />;
            default:
              <h1>Error displaying content.</h1>;
          }
        })()}
      </div>

      {/* Footer */}
      <div className="mt-3 flex w-full justify-center">
        {/* Fade */}
        <div className="pointer-events-none absolute right-0 bottom-0 h-15 w-full" style={{ background: `linear-gradient(to top, rgba(249, 250, 251, 1), transparent)` }} />

        {/* ADD FOOTER CONTENT HERE */}
      </div>
    </>
  );
}
