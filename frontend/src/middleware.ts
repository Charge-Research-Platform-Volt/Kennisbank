import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export async function middleware(request: NextRequest) {
    try {
        const response = await fetch("http://backend:8080/api/auth/status", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                Cookie: request.headers.get("cookie") || "", 
            },
        });

        if (!response.ok) {
            console.warn("Auth check failed:", response.status);
            return NextResponse.redirect(new URL("/login", request.url));
        }

        const data = await response.json();

        if (!data.isAuthenticated) {
            return NextResponse.redirect(new URL("/login", request.url));
        }

        return NextResponse.next();
    } catch (error) {
        console.error("Error checking auth:", error);
        return NextResponse.redirect(new URL("/login", request.url));
    }
}

export const config = {
    matcher: ["/((?!api|_next/static|_next/image|favicon.ico|login|signup).*)"],
};
