import { useSidebar } from "@/context/sidebar-provider";
import { cn } from "@/lib/utils";

export function Sidebar({
  side = "left",
  width = "300px",
  className,
  children,
  ...props
}: React.ComponentProps<"div"> & {
  side: "left" | "right";
  width?: string;
}) {
  const { leftSidebarState, rightSidebarState } = useSidebar();
  const state = side === "left" ? leftSidebarState : rightSidebarState;

  return (
    <div
      className="group peer text-sidebar-foreground hidden md:block"
      data-state={state}
      data-collapsible={state === "collapsed" ? "offcanvas" : ""}
      data-variant="sidebar"
      data-side={side}
      data-slot="sidebar"
      style={{ "--sidebar-width": width } as React.CSSProperties}
    >
      {/* This is what handles the sidebar gap on desktop */}
      <div
        data-slot="sidebar-gap"
        className={cn("relative w-(--sidebar-width) bg-transparent transition-[width] duration-150 ease-linear", "group-data-[collapsible=offcanvas]:w-0", "group-data-[side=right]:rotate-180")}
      />
      <div
        data-slot="sidebar-container"
        className={cn(
          "fixed inset-y-0 z-10 hidden h-svh w-(--sidebar-width) transition-[left,right,width] duration-150 ease-linear md:flex",
          side === "left" ? "left-0 group-data-[collapsible=offcanvas]:left-[calc(var(--sidebar-width)*-1)]" : "right-0 group-data-[collapsible=offcanvas]:right-[calc(var(--sidebar-width)*-1)]",
          "group-data-[side=left]:border-r group-data-[side=right]:border-l",
          className,
        )}
        {...props}
      >
        <div data-sidebar="sidebar" data-slot="sidebar-inner" className="bg-sidebar flex h-full w-full flex-col">
          {children}
        </div>
      </div>
    </div>
  );
}
