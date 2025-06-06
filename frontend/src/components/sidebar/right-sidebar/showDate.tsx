"use client"

import React, { JSX, useEffect } from 'react';
import { useSidebar } from "@/context/sidebar-provider";

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
    const { currentId } = useSidebar();

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