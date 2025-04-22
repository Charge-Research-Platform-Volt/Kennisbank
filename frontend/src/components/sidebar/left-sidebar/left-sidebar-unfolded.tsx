"use client";

import React from "react";
import Link from "next/link";
import SidebarPart from "./sidebar-part";
import NewButton from "@/components/sidebar/left-sidebar/sidebar-new-button";
import type { TagArray } from "@/types/tag.type";
import type { SidebarItem } from "@/types/sidebar";
import HideMenu from "@/icons/menu/hide-menu";
import ProfileDropdown from "./profile-dropdown";
import Divider from "../divider";
import QuickSearchButton from "./quick-search-button";

/**
 * 
 * @param menuItems - Menu items to display (home, archive, tags)
 * @param projects - Recent projects to display
 * @param bottomMenuItems - Items at the bottom of the sidebar (settings, help)
 * @param tags - Fetch of all tags
 * @param userEmail - Email of the current user
 * @param userRole - Role of the current user
 * @param switchMenuAction - Action that switches from the folded sidebar to the unfolded
 * @param handleLogoutAction - Action that logs out
 * @returns The full left sidebar, which takes up more space than the folded one, but is also in more detail. Has all functionality of the left sidebar, including for example quick search.
 */
export default function LeftSidebarUnfolded({ menuItems, projects, bottomMenuItems, tags, userEmail, userRole, switchMenuAction, handleLogoutAction }: { menuItems: SidebarItem[], projects: SidebarItem[], bottomMenuItems: SidebarItem[], tags: TagArray; userEmail: string; userRole: string, switchMenuAction: () => void, handleLogoutAction: () => void }) {
  return (
    <aside className="flex h-screen flex-col bg-gray-100 p-2.5 transition-all duration-300 overflow-y-auto w-64 pb-0">
        <div className="mb-4 flex items-center justify-between">
            {/* KnowledgeBase title */}
            <h2 className="text-xl font-semibold">
                <Link href="/">KnowledgeBank</Link>
            </h2>

            {/* Hide menu button */}
            <button type="button" data-testid="sidebar_hide" onClick={() => switchMenuAction()} className="rounde rounded-lg p-2 text-white transition hover:bg-gray-200" style={{ cursor: "pointer" }}>
                <HideMenu className="h-6 w-6" />
            </button>
        </div>

        {/* Menu items */}
        <nav className="flex-grow flex-col mb-[1vh]">
            <div className="space-y-2">
                <NewButton data-testid="sidebar" tags={tags} />

                {/* Search bar */}
                <QuickSearchButton data-testid="sidebar" />
            </div>
        </nav>
            
        <nav className="flex-grow overflow-y-auto h-full min-h-20">
            {/* Menu and project parts */}
            <SidebarPart name="Menu" items={menuItems} />
            <SidebarPart name="Projects" items={projects} />
        </nav>

        {/* Bottom items of the menu */}
        <nav>
            <ul>
                {bottomMenuItems.map((item, index) => (
                    <li key={index}>
                        <Link data-testid="sidebar" href={item.path} className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
                            {item.icon}
                            {item.name}
                        </Link>
                    </li>
                ))}
                <Divider />
                <li>
                    <ProfileDropdown isIcon={false} handleLogoutAction={handleLogoutAction} userEmail={userEmail} userRole={userRole} />
                </li>
                <li>
                    <label className="text-gray-400 text-xs"><i>©Utrecht University (ICS)</i></label>
                </li>
            </ul>
        </nav>
    </aside>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
