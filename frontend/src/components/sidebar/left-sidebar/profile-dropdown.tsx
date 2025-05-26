"use client";

import React, { useEffect, useState } from "react";
import Image from "next/image";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { useRouter } from "next/navigation";

/**
 *
 * @param asIcon - Boolean indicating whether or not to render the account details on the rest of the button (email and name)
 * @param handleLogoutAction - Action to log out
 * @param userEmail - Email of the user
 * @param userRole - Current role of the user
 * @returns The profile dropdown, which consists of a clickable profile picture, able to take the user to their profile or log out.
 */
export default function ProfileDropdown({ handleLogoutAction, userEmail = "", userName = "", minimize }: { handleLogoutAction: () => void; userEmail?: string; userName?: string; minimize: boolean }) {
  const [profileButtonWidth, setProfileButtonWidth] = useState(0);
  const profileButtonRef = React.useRef<HTMLButtonElement>(null);

  const router = useRouter();

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (profileButtonRef.current) {
        setProfileButtonWidth(profileButtonRef.current.offsetWidth);
      }
    }, 150);

    return () => clearTimeout(timeoutId);
  }, [minimize]);

  return (
    <DropdownMenu>
      {/* Trigger button (profile photo and if not asIcon: username+email) */}
      <DropdownMenuTrigger
        ref={profileButtonRef}
        className={`font-face text-md data-[state=closed]:ring-0, flex w-full cursor-pointer items-center gap-2 rounded-md p-2 text-black outline-none hover:bg-gray-200 ${minimize && "w-9"} overflow-hidden transition-all duration-200`}
      >
        {/* Profile photo */}
        <Image src={`/img/default-profile-picture.svg`} alt="Profile Picture" width={35} height={35} />

        {/* Username + email */}
        <div className="flex flex-col items-start justify-center">
          <p className="text-sm">{userName}</p>
          <p className="text-sm text-gray-500">{userEmail}</p>
        </div>
      </DropdownMenuTrigger>

      {/* Dropdown menu */}
      <DropdownMenuContent side="top" style={{ width: profileButtonWidth }}>
        <DropdownMenuItem onClick={() => router.push("/account")} className="cursor-pointer">
          Account settings
        </DropdownMenuItem>
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
