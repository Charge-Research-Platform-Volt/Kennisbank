"use client";

import React, { useState } from "react";
import type { TagArray } from "@/types/tag.type";
import type { SidebarItem } from "@/types/sidebar";
import { Logout } from "@/actions/authActions";
import Cookies from 'js-cookie';
import LeftSidebarFolded from "./left-sidebar-folded";
import LeftSidebarUnfolded from "./left-sidebar-unfolded";

/**
 * 
 * @param tags - All tags fetched from backend
 * @param userEmail - Email of the user
 * @param userRole - Role of the user (admin or user currently)
 * @param open - Boolean determining whether the side bar is open or folded
 * @param menuItems - Menu items to display (home, archive, tags)
 * @param projects - Recent projects to display
 * @param bottomMenuItems - Items at the bottom of the sidebar (settings, help)
 * @summary - This function makes from the fetches in the server component the actual left side bar
 * @returns The left side bar
 */
export default function LeftSidebarClient({ open, tags, userEmail, userRole, menuItems, projects, bottomMenuItems }: { open: boolean, tags: TagArray; userEmail: string; userRole: string, menuItems: SidebarItem[], projects: SidebarItem[], bottomMenuItems: SidebarItem[] }) {
  const [isOpen, setIsOpen] = useState(open); 

  const handleLogout = async () => {
    const result = await Logout();

    if (result.success) window.location.reload();
    else console.error(result.message);
  };

  const switchMenuAction = () => {
    Cookies.set('menuOpened', !isOpen); 
    setIsOpen(!isOpen);
  }

  return (
    <div>
      {/* Show small menu bar if menu is closed */}
      {!isOpen && (
        <LeftSidebarFolded 
          menuItems={menuItems} 
          projects={projects} 
          bottomMenuItems={bottomMenuItems}
          tags={tags} 
          handleLogoutAction={handleLogout} 
          switchMenuAction={switchMenuAction} 
        />
      )}

      {/* Show menu if it is opened */}
      {isOpen && (
        <LeftSidebarUnfolded 
          menuItems={menuItems} 
          projects={projects} 
          bottomMenuItems={bottomMenuItems}
          tags={tags} 
          userEmail={userEmail} 
          userRole={userRole} 
          handleLogoutAction={handleLogout} 
          switchMenuAction={switchMenuAction} 
        />
      )}
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


