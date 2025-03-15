import Link from "next/link";
import Logo from "./logo";

export default function Navbar() {
    return (
        <nav className="flex items-center justify-between py-4">
            <Logo size="medium" />
            <ul className="flex items-center gap-10">
                <Item href="/">Home</Item>
                <Item href="/guide">Guide</Item>
                <Item href="/docs">Docs</Item>
                <Item href="/changelog">Changelog</Item>
            </ul>
        </nav>
    );
}

export function Item({
    href,
    children,
}: {
    href: string;
    children: React.ReactNode;
}) {
    return (
        <li>
            <Link
                href={href}
                className="rounded-sm px-2 py-1.5 font-bold transition-colors hover:bg-gray-100"
            >
                {children}
            </Link>
        </li>
    );
}
