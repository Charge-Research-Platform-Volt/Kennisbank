import { JSX } from "react";

export interface SidebarItem {
  id: number;
  name: string;
  path: string;
  icon: JSX.Element;
}
