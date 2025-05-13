"use client"

import React from "react";
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from "@/components/ui/collapsible";
import { ChevronsUpDown } from "lucide-react";

interface ExpandableProps 
{
    title: string;
    children?: React.ReactNode;
    collapsedHeight?: number;
    fadeColor?: string;
    variant?: "vertical" | "horizontal";
}

export default function Expandable({
    title, 
    children, 
    collapsedHeight = 50,
    fadeColor = "bg-gray-50", // Default to sidebar background
    variant = "vertical"
}: ExpandableProps) 
{
    const [open, setOpen] = React.useState<boolean>(false);
    
    const isHorizontal = variant === "horizontal";
    
    // Map Tailwind color classes to RGB values for gradients
    const tailwindToRgb: Record<string, string> = {
        "bg-white": "255, 255, 255",
        "bg-gray-50": "249, 250, 251",
        "bg-gray-100": "243, 244, 246",
        "bg-gray-200": "229, 231, 235",
        "bg-blue-50": "239, 246, 255",
        "bg-red-50": "254, 242, 242",
        "bg-green-50": "240, 253, 244",
        "bg-yellow-50": "254, 252, 232",
    };
    
    // Get the RGB value for the gradient
    // If fadeColor is a direct color value (not a Tailwind class), use it directly
    const rgbValue = tailwindToRgb[fadeColor] || 
                    (fadeColor.startsWith("bg-") ? "255, 255, 255" : fadeColor);
    
    // Create the gradient color based on whether it's a Tailwind class or direct color value
    const gradientColor = fadeColor.startsWith("bg-") 
        ? `rgba(${rgbValue}, 1)` 
        : fadeColor;
    
    return (
        <div className="w-full relative">
            <Collapsible open={open} onOpenChange={setOpen} className="w-full">
                <CollapsibleTrigger className="w-full cursor-pointer">
                    <div className="w-full flex items-center gap-2">
                        <span>{title}</span>
                        <div className="flex-1 h-px bg-gray-300" />
                        <ChevronsUpDown className="h-4 w-4 text-gray-500" />
                    </div>
                </CollapsibleTrigger>
                
                <CollapsibleContent>
                    <div className={`w-full mt-2 ${isHorizontal ? "flex flex-wrap gap-2" : ""}`}>
                        {children}
                    </div>
                </CollapsibleContent>
            </Collapsible>
            
            {!open && !isHorizontal && (
                // Vertical variant
                <div 
                    className="relative mt-2 w-full overflow-hidden"
                    style={{ maxHeight: `${collapsedHeight}px` }}
                >
                    <div className="w-full">
                        {children}
                    </div>
                    {/* Vertical fade at bottom */}
                    <div 
                        className="absolute inset-x-0 bottom-0 h-12 pointer-events-none"
                        style={{ 
                            background: `linear-gradient(to top, ${gradientColor}, transparent)` 
                        }}
                    ></div>
                </div>
            )}
            
            {!open && isHorizontal && (
                // Horizontal variant
                <div className="relative mt-2 w-full">
                    <div 
                        className="overflow-hidden"
                        style={{ maxHeight: `${collapsedHeight}px` }}
                    >
                        <div className="w-full whitespace-nowrap overflow-x-hidden">
                            <div className="inline-flex gap-2">
                                {children}
                            </div>
                        </div>
                    </div>
                    {/* Horizontal fade at right */}
                    <div 
                        className="absolute inset-y-0 right-0 w-16 pointer-events-none"
                        style={{ 
                            background: `linear-gradient(to left, ${gradientColor}, transparent)` 
                        }}
                    ></div>
                </div>
            )}
        </div>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
