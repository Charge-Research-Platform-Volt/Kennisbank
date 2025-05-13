"use client";

import React from "react";
import { Sidebar } from "@/components/ui/sidebar";
import { useSidebar } from "@/context/sidebar-provider";
import { MetadataTypeEnum } from "@/context/sidebar-provider";
import { ResourceContent } from "./ResourceContent";
import { PersonContent } from "./PersonContent";
import { OrganisationContent } from "./OrganisationContent";
import { Button } from "@/components/ui/button";
import HideMenu from "@/icons/menu/hide-menu";

/**
 *
 * @returns The right sidebar visible when clicked on an item in the archive. Displays useful information such as metadata and related files (soon).
 */
export default function RightSidebar()
{
  	const { currentId, currentType, navigateBack, navigateForward, setRightSidebarOpen, isEmptyPrevs, isEmptyNexts } = useSidebar();

	const handleOpenClick = () => 
	{
		console.log("OPENING!");
	}

  	return (
		<Sidebar side="right" width="40rem" collapsible="offcanvas">
			{/* Navigation buttons */}
			<div className="w-full flex justify-between p-4 space-x-2">
				{/* Hide menu button */}
				<Button data-testid="sidebar_hide" variant="outline" size="icon" onClick={() => { setRightSidebarOpen(false); }}><HideMenu flip={true} /></Button>
			
				<Button variant="outline" onClick={handleOpenClick} className="flex-1">Download</Button>
				
				<div className="flex justify-center space-x-2">
					<Button variant="outline" onClick={navigateBack} className="w-[8rem]" disabled={isEmptyPrevs()}>Previous</Button>
					<Button variant="outline" onClick={navigateForward} className="w-[8rem]" disabled={isEmptyNexts()}>Next</Button>
				</div>
			</div>
			
			{/* Content area */}
			<div className="w-full h-full flex-1 overflow-y-auto p-4">
				{(() => 
				{
				    switch (currentType) 
				    {
				        case MetadataTypeEnum.RESOURCE:
							return <ResourceContent />
						case MetadataTypeEnum.PERSON:
							return <PersonContent />
						case MetadataTypeEnum.ORGANISTATION:
							return <OrganisationContent />
						default:
							<h1>Error displaying content.</h1>
				    }
				})()}
			</div>
			
			{/* Footer */}
			<div className="w-full flex justify-center p-3">
				<p>Footer content here</p>
			</div>
		</Sidebar>
	);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
