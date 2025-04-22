import { cn } from "@/lib/utils";
import Image from "next/image";
import React from "react";

export default function Logo({ size }: { size: "small" | "medium" }) {
    const logoSize =
        size === "small" ? " h-[40px] w-[100px]" : "h-[50px] w-[120px]";
    return (
        <div className={cn("relative top-0 left-0", logoSize)}>
            <Image
                src="/charge-logo.png"
                alt="Logo"
                fill
                className="object-contain"
            />
        </div>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


