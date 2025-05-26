import { useSidebar } from "@/context/sidebar-provider";
import { cn } from "@/lib/utils";

export function Sidebar({
  side = "left",
  width = "300px",
  collapsedWidth = "48px",
  collapsible = "offcanvas",
  className,
  children,
  menuRef,
  ...props
}: React.ComponentProps<"div"> & {
  side: "left" | "right";
  collapsible: "offcanvas" | "icon";
  width?: string;
  collapsedWidth?: string;
  menuRef?: React.RefObject<HTMLDivElement|null>;
}) {
  const { leftSidebarState, rightSidebarState } = useSidebar();
  const state = side === "left" ? leftSidebarState : rightSidebarState;

  return (
    <div
      className="group peer text-sidebar-foreground md:block"
      data-state={state}
      data-collapsible={state === "collapsed" ? collapsible : ""}
      data-variant="sidebar"
      data-side={side}
      data-slot="sidebar"
      style={{ "--sidebar-width": width, "--sidebar-width-icon": collapsedWidth } as React.CSSProperties}
      ref={menuRef}
    >
      {/* This is what handles the sidebar gap on desktop */}
      <div
        data-slot="sidebar-gap"
        className={cn(
          "relative w-[var(--sidebar-width)] bg-transparent transition-[width] duration-150 ease-linear",
          "group-data-[collapsible=offcanvas]:w-0",
          "group-data-[side=right]:rotate-180",
          "group-data-[collapsible=icon]:w-[var(--sidebar-width-icon)]",
        )}
      />

      <div
        data-slot="sidebar-container"
        className={cn(
          "fixed inset-y-0 z-10 h-svh w-[var(--sidebar-width)] transition-[left,right,width] duration-150 ease-linear md:flex",

          // Collapsed state
          side === "left" ? "left-0 group-data-[collapsible=offcanvas]:left-[calc(var(--sidebar-width)*-1)]" : "right-0 group-data-[collapsible=offcanvas]:right-[calc(var(--sidebar-width)*-1)]",
          "group-data-[side=left]:border-r group-data-[side=right]:border-l",
          "group-data-[collapsible=icon]:w-[var(--sidebar-width-icon)]",
        )}
        {...props}
      >
        <aside
          data-sidebar="sidebar"
          data-slot="sidebar-inner"
          className={cn("bg-sidebar flex h-full w-full flex-col", "overflow-x-hidden", "overflow-x-auto", "transition-all duration-200", className)}
        >
          {children}
        </aside>
      </div>
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
