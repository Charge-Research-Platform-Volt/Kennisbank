"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Input } from "../ui/input";
import SidebarPart from "./sidebar-part";
import NewButton from "@/components/left-sidebar/sidebar-new-button";
import { TagsArray } from "@/types/tag.type";
import Settings from "@/icons/settings";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import Home from "@/icons/home";
import { SidebarItem } from "@/types/sidebar";
import ProjectIcon1 from "@/icons/project-icons/icon-1";
import Divider from "../divider";
import Help from "@/icons/help";
import HideMenu from "@/icons/menu/hide-menu";
import ShowMenu from "@/icons/menu/show-menu";

// Menu items
const menuItems: SidebarItem[] = [
  { id: 1, name: "Home", path: "/", icon: <Home className="w-4 h-4" /> },
  { id: 2, name: "Tags", path: "/standardizedtags", icon: <Tags className="w-4 h-4" /> },
  { id: 3, name: "Archive", path: "/archive", icon: <Archive className="w-4 h-4" /> },
];

// Projects
const projects: SidebarItem[] = [
  {
    id: 1,
    path: "/projects",
    icon: <ProjectIcon1 className="w-4 h-4" />,
    name: "Project 1",
  },
];

export default function LeftSidebarClient({ tags }: { tags: TagsArray }) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <div>
      {/* Show menu button if menu is closed */}
      {!isOpen && (
        <div className="mt-4 mb-4 flex items-center justify-between">
          <button
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
        <aside className={`sticky top-0 flex h-screen flex-col bg-gray-100 p-2.5 transition-all duration-300 ${isOpen ? "w-64 bg-gray-100" : "w-16 bg-transparent"}`}>
          <div className="mb-4 flex items-center justify-between">
            {/* KnowledgeBase title */}
            <h2 className="text-xl font-semibold">
              <Link href="/">KnowledgeBase</Link>
            </h2>

            {/* Hide menu button */}
            <button onClick={() => setIsOpen(!isOpen)} className="rounde rounded-lg p-2 text-white transition hover:bg-gray-200" style={{ cursor: "pointer" }}>
              <HideMenu className="h-6 w-6" />
            </button>
          </div>

          {/* Menu items */}
          <nav className="flex h-full flex-col">
            <div className="mb-10 space-y-2">
              <NewButton tags={tags} />

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
                  <Settings className="w-4 h-4" />
                  Settings
                </Link>
              </li>

              <li>
                <Link href="http://localhost:3001/guide" className="flex items-center gap-x-2 rounded-xl p-2 hover:bg-gray-200">
                  <Help className="w-4 h-4" />
                  Help
                </Link>
              </li>

              <Divider />

              <li>
                <Link href="/settings/profile" className="flex rounded-xl p-2 hover:bg-gray-200">
                  <Image src="/img/default-profile-picture.svg" alt="Help" width={24} height={24} className="mr-2" />
                  <div>
                    <p className="text-sm">Name</p>
                    <p className="text-sm text-gray-500">example@gmail.com</p>
                  </div>
                </Link>
              </li>
            </ul>
          </nav>
        </aside>
      )}
    </div>
  );
}
