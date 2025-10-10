"use client";

import React from "react";
import { Sidebar } from "@/components/ui/sidebar";
import { ResourceContent } from "./ResourceContent";
import { PersonContent } from "./PersonContent";
import { OrganisationContent } from "./OrganisationContent";
import { Button } from "@/components/ui/button";
import HideMenu from "@/icons/menu/hide-menu";
import { ArrowLeft, ArrowRight, Calendar, Clock, Download, Pencil, PencilOff } from "lucide-react";
import ShowDate from './showDate';
import { handleOpenFile } from "@/actions/openFileActionsClient";
import { useArchiveSidebar, MetadataTypeEnum } from "@/context/archive-sidebar-provider";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

/**
 *
 * @returns The right sidebar visible when clicked on an item in the archive. Displays useful information such as metadata and related files.
 */
export default function ArchiveSidebar()
{
    const { currentType, creationDate, publicationDate, currentId, archiveSidebarOpen, navigateBack, navigateForward, setArchiveSidebarOpen, isEmptyPrevs, isEmptyNexts, setEditMode, editMode } = useArchiveSidebar();

    const [isLoading, setLoading] = React.useState<boolean>(false);
    const [fileType, setFileType] = React.useState<string | null>(null);


    const handleOpenClick = async () => 
    {
        setLoading(true);
        await handleOpenFile(currentId, fileType as string)
        setLoading(false);
    }

    

    return (
        <Sidebar side="right" width="40rem" collapsible="offcanvas" open={archiveSidebarOpen}>
            {/* Header */}
            <div className="w-full flex justify-between p-4 space-x-2">
                {/* Hide menu button */}
                <Tooltip delayDuration={700}>
                    <TooltipTrigger asChild>
                        <Button data-testid="sidebar_hide" variant="outline" size="icon" onClick={() => { setArchiveSidebarOpen(false); }}>
                            <HideMenu flip={true} />
                        </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                        <p>Hide sidebar</p>
                    </TooltipContent>
                </Tooltip>

                <div className="flex-1 space-x-2 flex justify-center">
                    <Tooltip delayDuration={700}>
                        <TooltipTrigger asChild>
                            <Button className="w-50" variant="outline" onClick={navigateBack} disabled={isEmptyPrevs()}>
                                <ArrowLeft />
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Navigate back</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    <Tooltip delayDuration={700}>
                        <TooltipTrigger asChild>
                            <Button className="w-50" variant="outline" onClick={navigateForward} disabled={isEmptyNexts()}>
                                <ArrowRight />
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Navigate forward</p>
                        </TooltipContent>
                    </Tooltip>
                </div>
                
                <Tooltip delayDuration={700}>
                    <TooltipTrigger asChild>
                        <Button variant="outline" onClick={handleOpenClick} disabled={(!isLoading) && (fileType) && currentType === MetadataTypeEnum.RESOURCE ? false : true}>
                            <Download />
                        </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                        <p>Download</p>
                    </TooltipContent>
                </Tooltip>
                
                <Tooltip delayDuration={700}>
                    <TooltipTrigger asChild>
                        <Button variant="outline" onClick={() => setEditMode(!editMode)}>
                            {!editMode && <Pencil />}
                            {editMode && <PencilOff />}
                        </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                        {!editMode && <p>Edit</p>}
                        {editMode && <p>Stop editting</p>}
                    </TooltipContent>
                </Tooltip>
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
            <div className="w-full flex justify-center my-3 gap-4 text-sm text-gray-700">
                {creationDate && (
                    <ShowDate date={creationDate}
                        text="Created"
                        icon={<Clock className="w-4 h-4 flex-shrink-0" />}
                        cName="flex items-center gap-2 min-w-0"
                    />
                )}
                
                {creationDate && publicationDate && (
                    <div className="w-px h-full bg-gray-300" />
                )}
                
                {publicationDate && (
                    <ShowDate date={publicationDate}
                        text="Published"
                        icon={<Calendar className="w-4 h-4 flex-shrink-0"/>}
                        cName="flex items-center gap-2 min-w-0"
                    />
                )}
            </div>
        </Sidebar>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
