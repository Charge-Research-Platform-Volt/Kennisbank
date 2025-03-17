import Logo from "@/components/logo";
import type { BaseLayoutProps } from "fumadocs-ui/layouts/shared";
import { Notebook, MonitorSmartphone, House } from "lucide-react";

export const baseOptions: BaseLayoutProps = {
    nav: {
        title: <Logo size="small" />,
        url: "/",
    },
    links: [
        {
            text: "Home",
            url: "/",
            active: "none",
            icon: <House />,
        },
        {
            text: "Guide",
            url: "/guide",
            active: "url",
            icon: <Notebook />,
        },
        {
            text: "Documentation",
            url: "/docs",
            active: "url",
            icon: <MonitorSmartphone />,
        },
    ],
};
