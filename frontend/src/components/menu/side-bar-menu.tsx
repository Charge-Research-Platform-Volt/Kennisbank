"use client"; 

import React, { useState } from "react";
import Link from "next/link";
import Image from 'next/image';
import { Input } from "../ui/input";
import MenuPart from "./menu-part";

const menuItems = [
  { name: "Home", path: "/", icon: "/img/home-icon.svg" },
  { name: "Standardized Tags", path: "/standardizedtags", icon: "/img/tags-icon.svg" },
];

const projects: Array<{id: string; path: string; icon: string; name: string}> = [{
  id: "1",
  path: "/projects",
  icon: "/img/placeholder-project.svg",
  name: "Placeholder project"
}];

const SideBarMenu: React.FC<{ className?: string }> = ({ className = "" }) => {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <div>
      {/* Show menu button if menu is closed */}
      {!isOpen && (
          <div className="flex items-center justify-between mb-4 mt-4">
              <button
              onClick={() => setIsOpen(!isOpen)}
              className="p-2 text-white rounded transition cursor-pointer bg-gray-100 rounded-r-lg rounded-l-none hover:bg-gray-200"
              style={{cursor: 'pointer'}}
              >
                  <Image 
                      src="/img/show-menu-icon.svg"
                      alt="Hide menu" 
                      width={24} 
                      height={24}
                  />
              </button>
          </div>
      )}

      {/* Show menu if it is opened */}
      {isOpen && (
        <aside className={`h-screen p-4 transition-all duration-300 flex flex-col ${isOpen ? "w-64 bg-gray-100" : "w-16 bg-transparent"} ${className}`}>
          <div className="flex items-center justify-between mb-4">
            {/* KnowledgeBase title */}
            <div className="flex items-center space-x-2">
                <h2 className="text-xl font-semibold">KnowledgeBase</h2>
            </div>
            
            {/* Hide menu button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="p-2 text-white rounde transition hover:bg-gray-200 rounded-lg"
                style={{cursor: 'pointer'}}
            >
              <Image 
                  src="/img/hide-menu-icon.svg"
                  alt="Hide menu" 
                  width={24} 
                  height={24}
              />
            </button>
          </div>

          {/* Menu items */}
          <nav className="flex flex-col h-full">
            <ul className="space-y-2">
              {/* New icon */}
              <li>
                  <Link 
                      href="/new"  
                      className="flex p-2 rounded-xl bg-purple hover:bg-purple/90 text-white"
                  >
                      <Image 
                          src="/img/new-icon.svg"
                          alt="Hide menu" 
                          width={24} 
                          height={24}
                          className="mr-2"
                      />
                      New
                  </Link>
              </li>

              {/* Search bar */}
              <Input
                  type="text"
                  name="search"
                  placeholder="&#x1F50E;&#xFE0E; Search"
              />
            </ul>

              {/* Menu and project parts */}
              <MenuPart name="Menu" items={menuItems} />
              <MenuPart name="Projects" items={projects} />
          </nav>

          {/* Bottom items of the menu */}
          <nav className="mt-auto">
            <ul>
              <li>
                <Link href="/settings" className="flex p-2 rounded-xl hover:bg-gray-200">
                  <Image src="/img/settings-icon.svg" alt="Settings" width={24} height={24} className="mr-2" />
                  Settings
                </Link>
              </li>
              <li>
                <Link href="http://localhost:3001/" className="flex p-2 rounded-xl hover:bg-gray-200">
                  <Image src="/img/help-icon.svg" alt="Help" width={24} height={24} className="mr-2" />
                  Help
                </Link>
              </li>
              <li className="flex items-center w-full" >
                    <div className="h-px bg-gray-300 flex-1 ml-2"></div>
              </li>
              <li>
                <Link href="/profile" className="flex p-2 rounded-xl hover:bg-gray-200">
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
};

export { SideBarMenu };
