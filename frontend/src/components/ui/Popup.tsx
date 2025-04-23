'use client';

import type React from "react";
import { cn } from "@/lib/utils";


function InputHeader({className, ...props}: React.ComponentProps<"h2">) {
    return (
        <h2 
        className={cn("text-xl font-semibold", className)}
        {...props}
        />
    );
}

function InputBlock({className, ...props}: React.ComponentProps<"div">) {
    return (
        <div 
        className={cn("mt-3", className)}
        {...props}
        />
    );
}

function FInput({className, type, name, ...props}: React.ComponentProps<"input">) {
    return (
        <input
        type={type}
        name={name}
        className={cn("bg-slate-200 h-10 pl-2", className)}
        {...props}
        />
    );
}

function FileInfo({className, ...props}: React.ComponentProps<"p">) {
    return (
        <p
        className={cn("text-s text-gray-500 pr-3", className)}
        {...props}
        />
    );
}

function PopupTitle({className, ...props}: React.ComponentProps<"h2">) {
    return (
        <h2
        className={cn("text-black font-bold text-2xl", className)}
        {...props}
        />
    );
}


export { InputHeader, InputBlock, FInput, FileInfo, PopupTitle};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


