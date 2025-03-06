"use client";

import React from "react";
import { MenuItem } from "@/types/menu-item";
import Image from 'next/image';
import { usePathname } from "next/navigation";
import Link from "next/link";

export default function MenuPart({ name, items }: { name: string; items: MenuItem[] }) {
    const pathname = usePathname();

    return (
        <ul>
            {/* Menu part title with line */}
            <li className="flex items-center w-full mt-10">
                <p className="text-gray-400 text-xs">{name}</p>
                <div className="h-px bg-gray-300 flex-1 ml-2"></div>
            </li>
            
            {/* Menu items */}
            {items.map((item) => (
                <li key={item.path}>
                    <Link   
                        href={item.path}
                        className={`flex p-2 rounded-xl ${
                        pathname === item.path ? "bg-white hover:bg-gray-200 shadow-sm" : "hover:bg-gray-200"
                        }`}
                    >
                        <Image 
                            src={item.icon}
                            alt={item.name} 
                            width={24} 
                            height={24}
                            className="mr-2"
                        />
                    {item.name}
                    </Link>
                </li>
            ))}
        </ul>
    );
}
