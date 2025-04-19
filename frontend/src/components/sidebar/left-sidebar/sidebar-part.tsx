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
export default function SidebarPart({ name, items }: { name: string; items: SidebarItem[] }) {
  const pathname : string = usePathname();

  return (
    <ul className="mb-10 flex flex-col gap-2">
      {/* Menu part title with line */}
      <Divider name={name} />

      {/* Menu items */}
      {items.map((item) => (
        <li key={item.id} data-testid = "sidebar">
          <Link href={item.path} className={`flex items-center gap-x-2 rounded-md p-2 hover:bg-gray-200 ${pathname === item.path ? "bg-white shadow-sm hover:bg-gray-200" : "hover:bg-gray-200"}`}>
            {item.icon}
            {item.name}
          </Link>
        </li>
      ))}
    </ul>
  );
}
