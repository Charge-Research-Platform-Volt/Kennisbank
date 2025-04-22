"use client";

import React, { useEffect, useState } from "react";
import Image from "next/image";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

/**
 * 
 * @param asIcon - Boolean indicating whether or not to render the account details on the rest of the button (email and name)
 * @param handleLogoutAction - Action to log out
 * @param userEmail - Email of the user
 * @param userRole - Current role of the user
 * @returns The profile dropdown, which consists of a clickable profile picture, able to take the user to their profile or log out.
 */
export default function ProfileDropdown({ isIcon = false, handleLogoutAction, userEmail = "", userRole = "" }: { isIcon?: boolean; handleLogoutAction: () => void; userEmail?: string; userRole?: string, }) {
  const [profileButtonWidth, setProfileButtonWidth] = useState(0);
  const profileButtonRef = React.useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (profileButtonRef.current) {
      setProfileButtonWidth(profileButtonRef.current.offsetWidth);
    }
  }, []);

  return (
    <DropdownMenu>
        { /* Trigger button (profile photo and if not asIcon: username+email) */ }
        <DropdownMenuTrigger
            ref={profileButtonRef}
            className={`font-face text-md flex h-auto w-full cursor-pointer items-center gap-1 rounded-xl bg-transparent p-2 text-left text-black shadow-none outline-none hover:bg-gray-200 data-[state=closed]:ring-0 ${isIcon ? "rounded-l-none" : ""}`}
        >
            { /* Profile photo */ }
            <Image src={`/img/default-profile-picture.svg`} alt="Help" width={24} height={24} className={`${isIcon ? "" : "mr-2"}`} />
            
            { /* Username + email */ }
            {!isIcon && (
                <div>
                    <p className="text-sm">{"Charge " + String(userRole).charAt(0).toUpperCase() + String(userRole).slice(1)}</p>
                    <p className="text-sm text-gray-500">{userEmail}</p>
                </div>
            )}
        </DropdownMenuTrigger>

    
        { /* Dropdown menu */ }
        <DropdownMenuContent side="top" style={{ width: profileButtonWidth }}>
            {/* <DropdownMenuItem className="cursor-pointer">
                Profile
            </DropdownMenuItem>
            <DropdownMenuSeparator /> */}
            <DropdownMenuItem onClick={handleLogoutAction} className="cursor-pointer">
                Log out
            </DropdownMenuItem>
        </DropdownMenuContent>
    </DropdownMenu>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)