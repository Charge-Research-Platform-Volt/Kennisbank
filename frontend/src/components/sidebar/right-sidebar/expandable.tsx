"use client"

import React, { JSX } from "react";
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from "@/components/ui/collapsible";
import { ChevronsUpDown } from "lucide-react";

interface ExpandableProps 
{
    title: string;
    children?: React.ReactNode;
    collapsedHeight?: number;
    fadeColor?: string;
    variant?: "vertical" | "horizontal";
    minHeight?: number;
    editButton?: JSX.Element;
}

export default function Expandable({
    title, 
    children, 
    collapsedHeight = 50,
    fadeColor = "bg-gray-50",
    variant = "vertical",
    minHeight = 30,
    editButton,
}: ExpandableProps) 
{
    const [open, setOpen] = React.useState<boolean>(false);
    const [contentHeight, setContentHeight] = React.useState<number>(0);
    const contentRef = React.useRef<HTMLDivElement>(null);
    
    const isHorizontal = variant === "horizontal";
    
    // Measure content height when component mounts or children change
    React.useEffect(() => {
        if (contentRef.current) {
            const height = contentRef.current.scrollHeight;
            setContentHeight(height);
        }
    }, [children]);
    
    // Only show fade if content is taller than minHeight and exceeds collapsedHeight
    const shouldShowFade = contentHeight > minHeight && contentHeight > collapsedHeight;
    
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
    
    const rgbValue = tailwindToRgb[fadeColor] || 
                    (fadeColor.startsWith("bg-") ? "255, 255, 255" : fadeColor);
    
    const gradientColor = fadeColor.startsWith("bg-") 
        ? `rgba(${rgbValue}, 1)` 
        : fadeColor;
    
    return (
        <div className="w-full relative mb-3">
            <Collapsible open={open} onOpenChange={setOpen} className="w-full">
                <CollapsibleTrigger className="w-full cursor-pointer">
                    <div className="w-full flex items-center gap-2">
                        <span>{title}</span>
                        <div className="flex-1 h-px bg-gray-300" />
                        <ChevronsUpDown className="h-4 w-4 text-gray-500" />
                    </div>
                </CollapsibleTrigger>
                
                <CollapsibleContent>
                    <div className={`w-full mt-2 select-none ${isHorizontal ? "flex flex-wrap gap-2" : ""}`}>
                        {children}
                    </div>

                    {editButton && (
                        <div className="mt-1 text-sm flex justify-center select-none">
                            {editButton}
                        </div>
                    )}
                </CollapsibleContent>
            </Collapsible>
            
            {!open && !isHorizontal && (
                <div 
                    className="relative mt-2 w-full overflow-hidden select-none"
                    style={{ maxHeight: `${collapsedHeight}px` }}
                >
                    <div className="w-full" ref={contentRef}>
                        {children}
                    </div>
                    {/* Only show vertical fade if content is tall enough */}
                    {shouldShowFade && (
                        <div 
                            className="absolute inset-x-0 bottom-0 h-12 pointer-events-none"
                            style={{ 
                                background: `linear-gradient(to top, ${gradientColor}, transparent)` 
                            }}
                        />
                    )}
                </div>
            )}
            
            {!open && isHorizontal && (
                <div className="relative mt-2 w-full select-none">
                    <div 
                        className="overflow-hidden"
                        style={{ maxHeight: `${collapsedHeight}px` }}
                    >
                        <div className="w-full whitespace-nowrap overflow-x-hidden">
                            <div className="inline-flex gap-2" ref={contentRef}>
                                {children}
                            </div>
                        </div>
                    </div>
                    {/* Only show horizontal fade if content is wide/tall enough */}
                    {shouldShowFade && (
                        <div 
                            className="absolute inset-y-0 right-0 w-16 pointer-events-none"
                            style={{ 
                                background: `linear-gradient(to left, ${gradientColor}, transparent)` 
                            }}
                        />
                    )}
                </div>
            )}
        </div>
    )
}