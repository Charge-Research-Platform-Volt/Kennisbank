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
import { Calendar, Clock } from "lucide-react";
import ShowDate from './showDate';
import { handleOpenFile } from "@/actions/openFileActionsClient";

/**
 *
 * @returns The right sidebar visible when clicked on an item in the archive. Displays useful information such as metadata and related files (soon).
 */
export default function RightSidebarInfo()
{
  	const { currentType, creationDate, publicationDate, currentId, navigateBack, navigateForward, setRightSidebarOpen, isEmptyPrevs, isEmptyNexts } = useSidebar();

	const [isLoading, setLoading] = React.useState<boolean>(false);
	const [fileType, setFileType] = React.useState<string | null>(null);


	const handleOpenClick = async () => 
	{
		setLoading(true);
		await handleOpenFile(currentId, fileType as string)
		setLoading(false);
	}

	

  	return (
		<Sidebar side="right" width="40rem" collapsible="offcanvas">
			{/* Navigation buttons */}
			<div className="w-full flex justify-between p-4 space-x-2">
				{/* Hide menu button */}
				<Button data-testid="sidebar_hide" variant="outline" size="icon" onClick={() => { setRightSidebarOpen(false); }}><HideMenu flip={true} /></Button>
			
				<Button variant="outline" onClick={handleOpenClick} disabled={(!isLoading) && (fileType) && currentType === MetadataTypeEnum.RESOURCE ? false : true} className="flex-1">Download</Button>
				
				<div className="flex justify-center space-x-2">
					<Button variant="outline" onClick={navigateBack} className="w-[8rem]" disabled={isEmptyPrevs()}>Previous</Button>
					<Button variant="outline" onClick={navigateForward} className="w-[8rem]" disabled={isEmptyNexts()}>Next</Button>
				</div>
			</div>
			
			{/* Content area */}
			<div className="w-full h-full flex-1 overflow-y-auto p-4 pb-25">
				{(() => 
				{
				    switch (currentType) 
				    {
				        case MetadataTypeEnum.RESOURCE:
							return <ResourceContent fileType={fileType} setFileType={setFileType}  />
						case MetadataTypeEnum.PERSON:
							return <PersonContent />
						case MetadataTypeEnum.ORGANISATION:
							return <OrganisationContent />
						default:
							<h1>Error displaying content.</h1>
				    }
				})()}
			</div>
			
			{/* Footer */}
			<div className="w-full flex justify-center mt-3">
				{publicationDate && creationDate ? (
					<div className="flex items-center justify-between gap-4 text-sm text-gray-700 mb-2">
						<ShowDate date={creationDate}
								  text="Created"
								  icon={<Clock className="w-4 h-4 flex-shrink-0 text-gray-700" />}
								  cName="flex items-center gap-2 min-w-0"/>

						<div className="w-px h-8 bg-gray-200" />
						<ShowDate date={publicationDate}
								text="Published"
								icon={<Calendar className="w-4 h-4 flex-shrink-0 text-gray-700"/>}
								cName="flex items-center gap-2 min-w-0"/>
					</div>
				) : creationDate && (
					<div className="flex items-center justify-between gap-4 text-sm text-gray-700 mb-2">
						<ShowDate date={creationDate}
									text="Created"
									icon={<Clock className="w-4 h-4 flex-shrink-0 text-gray-700" />}
									cName="flex items-center gap-2 min-w-0"/>
					</div>
				)
				}

			</div>
		</Sidebar>
	);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
