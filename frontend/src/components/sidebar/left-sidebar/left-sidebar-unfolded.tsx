"use client";

import React from "react";
import Link from "next/link";
import SidebarPart from "./sidebar-part";
import NewButton from "@/components/sidebar/left-sidebar/sidebar-new-button";
import type { TagArray } from "@/types/tag.type";
import Settings from "@/icons/settings";
import type { SidebarItem } from "@/types/sidebar";
import Help from "@/icons/help";
import HideMenu from "@/icons/menu/hide-menu";
import QuickSearch from "@/components/quick-search";
import ProfileDropdown from "./profile-dropdown";
import Divider from "../divider";

export default function LeftSidebarUnfolded({ menuItems, projects, settingsPath, tags, userEmail, userRole, switchMenuAction, handleLogoutAction }: { menuItems: SidebarItem[], projects: SidebarItem[], settingsPath: string, tags: TagArray; userEmail: string; userRole: string, switchMenuAction: () => void, handleLogoutAction: () => void }) {
  return (
    <aside className="flex h-screen flex-col bg-gray-100 p-2.5 transition-all duration-300 overflow-y-auto w-64">
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
        <nav className="flex-grow flex-col">
            <div className="space-y-2 mb-[2vh]">
                <NewButton tags={tags} />

                {/* Search bar */}
                <QuickSearch />
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
                <li>
                    <Link data-testid="sidebar" href={settingsPath} className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
                        <Settings className="h-4 w-4" />
                        Settings
                    </Link>
                </li>

                <li>
                    <Link data-testid="sidebar" href="http://localhost:3001/guide" className="flex items-center gap-x-2 rounded-xl p-2 hover:bg-gray-200">
                        <Help className="h-4 w-4" />
                        Help
                    </Link>
                </li>
                <Divider />
                <li>
                    <ProfileDropdown isIcon={false} handleLogoutAction={handleLogoutAction} userEmail={userEmail} userRole={userRole} />
                </li>
            </ul>
        </nav>
    </aside>
  );
}
