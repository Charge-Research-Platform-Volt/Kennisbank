"use client";

import React from "react";
import { usePathname } from "next/navigation";
import Link from "next/link";
import Divider from "../divider";
import { SidebarItem } from "@/types/sidebar";

export default function SidebarPart({ name, items }: { name: string; items: SidebarItem[] }) {
  const pathname = usePathname();

  return (
    <ul className="flex flex-col gap-2">
      {/* Menu part title with line */}
      <Divider name={name} />

      {/* menu items */}
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
