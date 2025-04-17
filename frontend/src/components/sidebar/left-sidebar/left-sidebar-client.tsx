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
import Divider from "../divider";
import Cookies from 'js-cookie';
import { usePathname } from "next/navigation";
import { useRouter } from 'next/navigation'

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

export default function LeftSidebarClient({ open, tags, userEmail, userRole }: { open: boolean, tags: TagArray; userEmail: string; userRole: string }) {
  const [isOpen, setIsOpen] = useState(open);
  const [profileButtonWidth, setProfileButtonWidth] = useState(0);
  const profileButtonRef = React.useRef<HTMLButtonElement>(null);
    
  const settingsPage = "/users";

  const pathname = usePathname();

  const router = useRouter();

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
      {/* Show small menu bar if menu is closed */}
      {!isOpen && (
        <div className="flex flex-col h-screen bg-gray-100 pt-2.5 pb-2.5 overflow-y-auto">
          <button
            type="button"
            data-testid="sidebar_hide"
            onClick={() => { Cookies.set('menuOpened', !isOpen); setIsOpen(!isOpen); }}
            className="cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 mb-4"
            style={{ cursor: "pointer" }}
          >
            <ShowMenu className="h-6 w-6" />
          </button>
          <NewButton tags={tags} asIcon={true} />
          <div className="flex flex-col overflow-y-auto min-h-15">
            {menuItems.map((item) => (
              <button
                key={item.id}
                type="button"
                data-testid="sidebar_hide"
                onClick={() => router.push(item.path)}
                className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200" ${pathname === item.path ? "bg-white shadow-sm hover:bg-gray-200" : "hover:bg-gray-200"}`}
                style={{ cursor: "pointer" }}
              >
                {item.icon}
              </button>
              )) }
              {projects.map((item) => (
              <button
                key={item.id}
                type="button"
                data-testid="sidebar_hide"
                onClick={() => router.push(item.path)}
                className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200" ${pathname === item.path ? "bg-white shadow-sm hover:bg-gray-200" : "hover:bg-gray-200"}`}
                style={{ cursor: "pointer" }}
              >
                {item.icon}
              </button>
              )) }
          </div>
          

            <div className="flex flex-col mt-auto">
              <button
                type="button"
                data-testid="sidebar_hide"
                onClick={() => router.push(settingsPage)}
                className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 w-full`}
                style={{ cursor: "pointer" }}
              >
                <Settings className="h-4 w-4" />
              </button>
              

              <button
                type="button"
                data-testid="sidebar_hide"
                onClick={() => router.push("http://localhost:3001/guide")}
                className={`cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 w-full`}
                style={{ cursor: "pointer" }}
              >
                <Help className="h-4 w-4" />
              </button>
              
              <DropdownMenu>
                    <DropdownMenuTrigger
                      ref={profileButtonRef}
                      className="cursor-pointer rounded rounded-l-none rounded-r-lg p-2 text-white transition hover:bg-gray-200 w-full"
                    >
                      <Image src="/img/default-profile-picture.svg" alt="Help" width={24} height={24} className="mr-2" />
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
            </div>
        </div>
      )}

      {/* Show menu if it is opened */}
      {isOpen && (
        <aside className={`flex h-screen flex-col bg-gray-100 p-2.5 transition-all duration-300 overflow-y-auto ${isOpen ? "w-64 bg-gray-100" : "w-16 bg-transparent"}`}>
          <div className="mb-4 flex items-center justify-between">
            {/* KnowledgeBase title */}
            <h2 className="text-xl font-semibold">
              <Link href="/">KnowledgeBank</Link>
            </h2>

            {/* Hide menu button */}
            <button type="button" data-testid="sidebar_hide" onClick={() => { Cookies.set('menuOpened', !isOpen); setIsOpen(!isOpen); }} className="rounde rounded-lg p-2 text-white transition hover:bg-gray-200" style={{ cursor: "pointer" }}>
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
                <Link data-testid="sidebar" href={settingsPage} className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200">
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
            </ul>
          </nav>
        </aside>
      )}
    </div>
  );
}
