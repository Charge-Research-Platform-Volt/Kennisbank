"use client";

import React from "react";
import { usePathname } from "next/navigation";
import Link from "next/link";
import Divider from "../divider";
import { SidebarItem } from "@/types/sidebar";

/**
 *
 * @param name - Name of the type of items displayed as this part
 * @param items - Items to display on the sidebar with an icon, name and path as most important attributes
 * @returns A part of the sidebar divided by a line
 */
export default function SidebarPart({ name, items, minimize }: { name: string; items: SidebarItem[]; minimize: boolean }) {
  const pathname: string = usePathname();

  return (
    <>
      {items.length > 0 && (
        <ul className="flex flex-col gap-2">
          {/* Menu part title with line */}
          <Divider name={name} minimize={minimize} />

          {/* menu items */}
          {items.map((item) => (
            <li key={item.id} data-testid="sidebar">
              <Link
                href={item.path}
                className={`flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200 ${pathname === item.path ? "border-input bg-background hover:bg-accent hover:text-accent-foreground border shadow-xs" : "border border-transparent hover:bg-gray-200"} overflow-hidden transition-all duration-200 ${!minimize && "w-9"} shrink-0 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-5`}
              >
                {item.icon}
                {item.name}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
