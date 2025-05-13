"use client";

import React from "react";
import type { SidebarItem } from "@/types/sidebar";
import { Logout } from "@/actions/authActions";
import { Sidebar } from "@/components/ui/sidebar";
import { Button } from "@/components/ui/button";
import { useSidebar } from "@/context/sidebar-provider";
import Link from "next/link";
import HideMenu from "@/icons/menu/hide-menu";
import New from "@/icons/new";
import QuickSearch from "@/components/quick-search";
import SidebarPart from "./sidebar-part";
import Divider from "../divider";
import ProfileDropdown from "./profile-dropdown";
import { cn } from "@/lib/utils";
import { usePathname, useSearchParams } from "next/navigation";

/**
 *
 * @param userEmail - Email of the user
 * @param userRole - Role of the user (admin or user currently)
 * @param open - Boolean determining whether the side bar is open or folded
 * @param menuItems - Menu items to display (home, archive, tags)
 * @param projects - Recent projects to display
 * @param bottomMenuItems - Items at the bottom of the sidebar (settings, help)
 * @summary - This function makes from the fetches in the server component the actual left side bar
 * @returns The left side bar
 */
export default function LeftSidebarClient({
  userEmail,
  userRole,
  menuItems,
  projects,
  bottomMenuItems,
}: {
  userEmail: string;
  userRole: string;
  menuItems: SidebarItem[];
  projects: SidebarItem[];
  bottomMenuItems: SidebarItem[];
}) {
  const { toggleLeftSidebar, leftSidebarOpen: open } = useSidebar();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const handleLogout = async () => {
    const result = await Logout();

    if (result.success) window.location.reload();
    else console.error(result.message);
  };
  
  const getCleanReturnUrl = () => {
    // Don't include returnUrl if we're already on the new page
    if (pathname === '/new') return null;
    
    // Strip any existing returnUrl parameters to avoid nesting
    const baseUrl = pathname.split('?')[0];
    const cleanParams = new URLSearchParams();
    
    // Only copy over non-returnUrl parameters
    for (const [key, value] of searchParams.entries()) {
      if (key !== 'returnUrl') {
        cleanParams.append(key, value);
      }
    }
    
    const paramString = cleanParams.toString();
    const cleanUrl = `${baseUrl}${paramString ? `?${paramString}` : ''}`;
    
    // Return null (don't add returnUrl param) if the return URL is just the homepage
    return cleanUrl === '/' ? null : cleanUrl;
  };

  // Then use this function in your Link
  const returnUrl = getCleanReturnUrl();
  const newPageUrl = returnUrl 
    ? `/new?returnUrl=${encodeURIComponent(returnUrl)}`
    : '/new';
    
  return (
    <Sidebar side="left" width="300px" collapsible="icon" className={`${open ? "p-2" : "px-1.5 pt-2"}`}>
      {/* Header */}
      <div className={"flex items-center justify-between pb-4"}>
        {/* KnowledgeBase title */}
        <Link href="/">
          <h2 className={cn("overflow-hidden text-xl font-semibold transition-all duration-300", !open && "max-w-0 opacity-0")}>KnowledgeBank</h2>
        </Link>

        {/* Hide menu button */}
        <Button
          data-testid="sidebar_hide"
          variant="outline"
          size="icon"
          onClick={() => {
            toggleLeftSidebar();
          }}
        >
          <HideMenu flipArrow={!open} />
        </Button>
      </div>

      {/* Menu items */}
      <nav className="mb-10 flex flex-col gap-2">
        {/* New button */}
        <Link href={newPageUrl}>
          <Button variant="default" className={`flex w-full items-center justify-start overflow-hidden p-2 transition-all duration-200 ${!open && "w-9"}`} data-testid="sidebar_new">
            <New className="h-4 w-4" />
            <div data-testid="button_text" className={`pb-0.5 ml-2 ${!open && "hidden"}`}>
              New
            </div>
          </Button>
        </Link>

        {/* Search bar */}
        <QuickSearch data-testid="sidebar" minimize={open} />
      </nav>

      {/* Menu and project parts */}
      <nav className="h-full min-h-20 flex-grow">
        <SidebarPart name="Menu" items={menuItems} minimize={open} />
        <SidebarPart name="Projects" items={projects} minimize={open} />
      </nav>

      {/* Bottom items of the menu */}
      <nav>
        <ul>
          {bottomMenuItems.map((item, index) => (
            <li key={index}>
              {/* <Link href={item.path} className="flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200"> */}
              <Link
                data-testid="sidebar"
                href={item.path}
                className={`flex items-center gap-x-2 overflow-hidden rounded-md p-2 hover:bg-gray-200 ${!open && "w-9"} shrink-0 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-5`}
              >
                {item.icon}
                {item.name}
              </Link>
            </li>
          ))}

          <Divider className="my-2" />

          {/* Settings and help */}
          <li>
            <ProfileDropdown handleLogoutAction={handleLogout} userEmail={userEmail} userRole={userRole} minimize={open} />
          </li>
        </ul>
      </nav>
    </Sidebar>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
