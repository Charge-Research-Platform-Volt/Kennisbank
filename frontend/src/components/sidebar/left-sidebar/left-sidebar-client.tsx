"use client";

import React, { useState } from "react";
import type { TagArray } from "@/types/tag.type";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import Home from "@/icons/home";
import type { SidebarItem } from "@/types/sidebar";
import ProjectIcon1 from "@/icons/project-icons/icon-1";
import { Logout } from "@/actions/authActions";
import Cookies from 'js-cookie';
import LeftSidebarFolded from "./left-sidebar-folded";
import AboutIcon from "@/icons/about-icon";
import LeftSidebarUnfolded from "./left-sidebar-unfolded";

// Menu items
const menuItems: SidebarItem[] = [
  { id: 1, name: "Home", path: "/", icon: <Home className="h-4 w-4" /> },
  { id: 2, name: "Archive", path: "/archive", icon: <Archive className="h-4 w-4" /> },
  { id: 3, name: "Tags", path: "/tags", icon: <Tags className="h-4 w-4" /> },
];

// Projects
const projects: SidebarItem[] = [
  {
    id: 1,
    path: "/projects",
    icon: <ProjectIcon1 className="h-4 w-4" />,
    name: "Project 1",
  },
];
/**
 * 
 * @param tags - All tags fetched from backend
 * @param userEmail - Email of the user
 * @param userRole - Role of the user (admin or user currently)
 * @param open - Boolean determining whether the side bar is open or folded
 * @summary - This function makes from the fetches in the server component the actual left side bar
 * @returns The left side bar
 */
export default function LeftSidebarClient({ open, tags, userEmail, userRole }: { open: boolean, tags: TagArray; userEmail: string; userRole: string }) {
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
          settingsPath="/users" 
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
          settingsPath="/users" 
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


