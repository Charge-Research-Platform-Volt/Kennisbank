"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import SidebarPart from "./sidebar-part";
import NewButton from "@/components/sidebar/left-sidebar/sidebar-new-button";
import type { TagArray } from "@/types/tag.type";
import Settings from "@/icons/settings";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import Home from "@/icons/home";
import type { SidebarItem } from "@/types/sidebar";
import ProjectIcon1 from "@/icons/project-icons/icon-1";
import Help from "@/icons/help";
import HideMenu from "@/icons/menu/hide-menu";
import ShowMenu from "@/icons/menu/show-menu";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Logout } from "@/actions/authActions";
import QuickSearch from "@/components/quick-search";
import AboutIcon from "@/icons/about-icon";
import Divider from "../divider";

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
 * @summary - This function makes from the fetches in the server component the actual left side bar
 * @returns The left side bar
 */
export default function LeftSidebarClient({ tags, userEmail, userRole }: { tags: TagArray; userEmail: string; userRole: string }) {
  const [isOpen, setIsOpen] = useState(true);
  const [profileButtonWidth, setProfileButtonWidth] = useState(0);
  const profileButtonRef = React.useRef<HTMLButtonElement>(null);

  const handleLogout = async () => {
    const result = await Logout();

    if (result.success) window.location.reload();
    else console.error(result.message);
  };

  useEffect(() => {
    if (profileButtonRef.current) {
      setProfileButtonWidth(profileButtonRef.current.offsetWidth);
    }
  }, []);

  return (
    <div>
      {/* Show menu button if menu is closed */}
      {!isOpen && (
        <div className="mt-4 mb-4 flex items-center justify-between">
          <button
            type="button"
            data-testid="sidebar_hide"
            onClick={() => setIsOpen(!isOpen)}
            className="cursor-pointer rounded rounded-l-none rounded-r-lg bg-gray-100 p-2 text-white transition hover:bg-gray-200"
            style={{ cursor: "pointer" }}
          >
            <ShowMenu className="h-6 w-6" />
          </button>
        </div>
      )}

      {/* Show menu if it is opened */}
      {isOpen && (
        <aside className={`flex h-screen flex-col bg-gray-100 p-2.5 transition-all duration-300 ${isOpen ? "w-64 bg-gray-100" : "w-16 bg-transparent"}`}>
          <div className="mb-4 flex items-center justify-between">
            {/* KnowledgeBase title */}
            <h2 className="text-xl font-semibold">
              <Link href="/">KnowledgeBank</Link>
            </h2>

            {/* Hide menu button */}
            <button type="button" data-testid="sidebar_hide" onClick={() => setIsOpen(!isOpen)} className="rounde rounded-lg p-2 text-white transition hover:bg-gray-200" style={{ cursor: "pointer" }}>
              <HideMenu className="h-6 w-6" />
            </button>
          </div>

          {/* Menu items */}
          <nav className="flex h-full flex-col">
            <div className="mb-10 space-y-2">
              <NewButton tags={tags} />

              {/* Search bar */}
              <QuickSearch />
            </div>

            {/* Menu and project parts */}
            <SidebarPart name="Menu" items={menuItems} />
            <SidebarPart name="Projects" items={projects} />
          </nav>

          {/* Bottom items of the menu */}
          <nav className="mt-auto">
            <ul>
              <li>
                <Link data-testid="sidebar" href="/users" className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
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

              <li>
                <Link data-testid="sidebar" href="/about" className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
                  <AboutIcon className="h-4 w-4" />
                    About us
                  </Link>
              </li>

              <Divider />

              <li>
                <DropdownMenu>
                  <DropdownMenuTrigger
                    ref={profileButtonRef}
                    className="font-face text-md flex h-auto w-full cursor-pointer items-center gap-1 rounded-xl bg-transparent p-2 text-left text-black shadow-none outline-none hover:bg-gray-200 data-[state=closed]:ring-0"
                  >
                    <Image src="/img/default-profile-picture.svg" alt="Help" width={24} height={24} className="mr-2" />
                    <div>
                      <p className="text-sm">{"Charge " + String(userRole).charAt(0).toUpperCase() + String(userRole).slice(1)}</p>
                      <p className="text-sm text-gray-500">{userEmail}</p>
                    </div>
                  </DropdownMenuTrigger>

                  <DropdownMenuContent side="top" style={{ width: profileButtonWidth }}>
                    <DropdownMenuItem className="cursor-pointer">
                      Profile
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={handleLogout} className="cursor-pointer">
                      Log out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </li>
              <li className="mt-2">
                {/* Copyright notice (bottom left) */}
                <label>
                    <i>©Utrecht University (ICS)</i>
                </label>
              </li>
            </ul>
          </nav>
        </aside>
      )}
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


