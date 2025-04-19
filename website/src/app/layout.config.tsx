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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


