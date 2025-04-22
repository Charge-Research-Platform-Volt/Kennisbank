"use client";

import React from "react";
import NewButton from "@/components/sidebar/left-sidebar/sidebar-new-button";
import type { TagArray } from "@/types/tag.type";
import type { SidebarItem } from "@/types/sidebar";
import ShowMenu from "@/icons/menu/show-menu";
import { usePathname } from "next/navigation";
import { useRouter } from 'next/navigation'
import ProfileDropdown from "./profile-dropdown";
import FoldedButton from "./folded-button";

/**
 * 
 * @param menuItems - Menu items to display (home, archive, tags)
 * @param projects - Recent projects to display
 * @param bottomMenuItems - Items at the bottom of the sidebar (settings, help)
 * @param Tags - Fetch of all tags
 * @param switchMenuAction - Action that switches from the folded sidebar to the unfolded
 * @param handleLogoutAction - Action that logs out
 * @returns The folded left sidebar, which takes up less space than the full one, but is also in less detail. Has most functionality of the normal sidebar.
 */
export default function LeftSidebarFolded({ menuItems, projects, bottomMenuItems, tags, switchMenuAction, handleLogoutAction }: { menuItems: SidebarItem[], projects: SidebarItem[], bottomMenuItems: SidebarItem[], tags: TagArray; switchMenuAction: () => void, handleLogoutAction: () => void }) {
  const pathname = usePathname();

  const router = useRouter();

  return (
    <div className="flex flex-col h-screen bg-gray-100 pt-2.5 pb-2.5 overflow-y-auto">
        {/* Button for opening menu */}
        <FoldedButton testid="sidebar_show" action={switchMenuAction} icon={<ShowMenu className="h-6 w-6" />} className="mb-4" />
        
        {/* New button */}
        <NewButton tags={tags} asIcon={true} />

        { /* Side bar items */ }
        <div className="flex flex-col overflow-y-auto min-h-15">
            { /* Menu items */ }
            {menuItems.map((item) => (
                <FoldedButton
                    testid="hiddensidebar" 
                    key={item.id} 
                    action={() => router.push(item.path)} 
                    icon={item.icon} 
                    className={pathname === item.path ? "bg-white shadow-sm hover:bg-gray-200" : "hover:bg-gray-200"} 
                />
            )) }

            { /* Projects */ }
            {projects.map((item) => (
                <FoldedButton
                    testid="hiddensidebar" 
                    key={item.id}
                    action={() => router.push(item.path)}
                    icon={item.icon}
                    className={pathname === item.path ? "bg-white shadow-sm hover:bg-gray-200" : "hover:bg-gray-200"}
                />
            )) }
        </div>
        
        { /* Bottom items */ }
        <div className="flex flex-col mt-auto">
            {bottomMenuItems.map((item) => (
                <FoldedButton
                    testid="hiddensidebar" 
                    key={item.id}
                    action={() => router.push(item.path)}
                    icon={item.icon}
                />
            )) }
            <ProfileDropdown isIcon={true} handleLogoutAction={handleLogoutAction} />
        </div>
    </div>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)