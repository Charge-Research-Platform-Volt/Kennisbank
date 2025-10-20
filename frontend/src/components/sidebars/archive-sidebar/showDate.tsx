"use client"

import React, { JSX, useEffect } from 'react';
import { useArchiveSidebar } from "@/context/archive-sidebar-provider";

export default function ShowDate({
    date,
    text,
    icon,
    cName,
} : {
    date: Date;
    text: string;
    icon: JSX.Element;
    cName: string;
})
{
    const { currentId } = useArchiveSidebar();

    useEffect(() => {
        
    }, [currentId])

    const formatDate = (date: Date) => {
        if (!date) return '';
        return new Intl.DateTimeFormat('en-US', {
          month: 'short',
          day: 'numeric',
          year: 'numeric'
        }).format(new Date(date));
      };


    return (
        <div className={cName}>
            {icon}
            <div className="truncate">
                <span className="text-gray-700">{text}</span>
                <div className="font-medium text-gray-700">
                    {formatDate(date)}
                </div>
            </div>
        </div>
    )
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
