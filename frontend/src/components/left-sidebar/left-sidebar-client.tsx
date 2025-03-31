"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Input } from "../ui/input";
import SidebarPart from "./sidebar-part";
import NewButton from "@/components/left-sidebar/sidebar-new-button";
import type { TagsArray, UserTagsArray } from "@/types/tag.type";
import Settings from "@/icons/settings";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import Home from "@/icons/home";
import type { SidebarItem } from "@/types/sidebar";
import ProjectIcon1 from "@/icons/project-icons/icon-1";
import Divider from "../divider";
import Help from "@/icons/help";
import HideMenu from "@/icons/menu/hide-menu";
import ShowMenu from "@/icons/menu/show-menu";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Logout } from "@/actions/authActions";

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

export default function LeftSidebarClient({ userTags, standardizedTags }: { userTags: UserTagsArray, standardizedTags: TagsArray }) {
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
              <Link href="/">KnowledgeBase</Link>
            </h2>

            {/* Hide menu button */}
            <button type="button" onClick={() => setIsOpen(!isOpen)} className="rounde rounded-lg p-2 text-white transition hover:bg-gray-200" style={{ cursor: "pointer" }}>
              <HideMenu className="h-6 w-6" />
            </button>
          </div>

          {/* Menu items */}
          <nav className="flex h-full flex-col">
            <div className="mb-10 space-y-2">
              <NewButton userTags={userTags} standardizedTags={standardizedTags} />

              {/* Search bar */}
              <Input type="text" name="search" placeholder="&#x1F50E;&#xFE0E; Search" />
            </div>

            {/* Menu and project parts */}
            <SidebarPart name="Menu" items={menuItems} />
            <SidebarPart name="Projects" items={projects} />
          </nav>

          {/* Bottom items of the menu */}
          <nav className="mt-auto">
            <ul>
              <li>
                <Link href="/settings" className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
                  <Settings className="h-4 w-4" />
                  Settings
                </Link>
              </li>

              <li>
                <Link href="http://localhost:3001/guide" className="flex items-center gap-x-2 rounded-xl p-2 hover:bg-gray-200">
                  <Help className="h-4 w-4" />
                  Help
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
                      <p className="text-sm">Name</p>
                      <p className="text-sm text-gray-500">example@gmail.com</p>
                    </div>
                  </DropdownMenuTrigger>

                  <DropdownMenuContent side="top" style={{ width: profileButtonWidth }}>
                    <DropdownMenuItem onClick={() => console.log("Profile clicked")} className="cursor-pointer">
                      Profile
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={handleLogout} className="cursor-pointer">
                      Log out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </li>
            </ul>
          </nav>
        </aside>
      )}
    </div>
  );
}
