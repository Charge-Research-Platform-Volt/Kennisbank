import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { z } from "zod";
import Help from "@/icons/help";
import { SidebarItem } from "@/types/sidebar";
import Home from "@/icons/home";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import { MessageCircleMore, Users } from "lucide-react";
import Projects from "@/icons/project-icons/icon-1";
import { UserDataSchema } from "@/types/user.type";

/**
 * @summary This function does the needed fetches from a server component and gives them to the client side component for use.
 * @returns Left side bar made from server component
 */
export default async function LeftSidebarServer() {
  const userFetch = await FetchWithValidation(UserDataSchema, `${process.env.API_URL}/user/current/account`);

  if (!userFetch.success) {
    console.log("Failed to fetch user data");
    console.log(userFetch);
    return <h1>ERROR</h1>;
  }

  const userRole = await FetchWithValidation(z.object({ role: z.string(), isAuthenticated: z.boolean() }), `${process.env.API_URL}/roles/current`);

  if (!userRole.success) {
    console.log("Failed to fetch user role");
    console.log(userRole);
    return <h1>ERROR</h1>;
  }

  // Menu items
  const menuItems: SidebarItem[] = [
    { id: 1, name: "Home", path: "/", icon: <Home className="h-4 w-4" /> },
    { id: 2, name: "Archive", path: "/archive", icon: <Archive className="h-4 w-4" /> },
    { id: 3, name: "Tags", path: "/tags", icon: <Tags className="h-4 w-4" /> },
    { id: 4, name: "ChatBot", path: "/chat", icon: <MessageCircleMore className="h-4 w-4" /> },
    { id: 5, name: "Projects", path: "/projects", icon: <Projects className="h-4 w-4" /> },
  ];

  // Projects
  const projects: SidebarItem[] = [];

  // Items at the bottom of the sidebar (help)
  const bottomMenuItems: SidebarItem[] = [
    {
      id: 1,
      path: `${process.env.HELP_URL}/guide`,
      icon: <Help className="h-4 w-4" />,
      name: "Help",
    },
  ];

  // If admin: add settings to the bottom menu items at index 0
  if (userRole.data.role === "admin") {
    menuItems.splice(menuItems.length, 0, {
      id: 6,
      path: "/users",
      icon: <Users color="black" className="h-4 w-4" />,
      name: "Users",
    });
  }

  return <LeftSidebarClient userData={userFetch.data} menuItems={menuItems} projects={projects} bottomMenuItems={bottomMenuItems} />;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
