'use client';

import { Icon } from '@iconify/react';
import { useState, useRef, useEffect, ChangeEvent } from "react";
import NewButton from './newDropdown';
import './sidebar.css';
import '@/app/globals.css'
import { DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger } from "@/components/ui/dropdown-menu";


export default function SideBar()
{


    return (
    <div className="sideBar pl-1 pr-1 pt-1">
        <NewButton />
    </div>
    );
}




