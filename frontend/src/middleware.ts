import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export async function middleware(request: NextRequest) {
    try {
        console.log(request.headers.get("cookie"))
        console.log(request.nextUrl.pathname)
        console.log(request.nextUrl.pathname.startsWith("/api/Auth/"))
        // Check in backend if logged in
        const response = await fetch("http://backend:8080/Auth/ping", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                Cookie: request.headers.get("cookie") || "", // Cookies meesturen!
            },
        });

        // If endpoint is /login or /signup, don't redirect if not logged in, redirect if logged in
        const pathname = request.nextUrl.pathname;
        if (pathname === "/login" || pathname === "/signup") {
            if (!response.ok) {
                console.warn("Auth check failed:", response.status);
                return NextResponse.next();
            }
                    
            return NextResponse.redirect(new URL("/", request.url));
        }

        // One exception, so the client can call the api to create cookies
        if (pathname.startsWith("/api/Auth/")) { return NextResponse.next() }

        // If not logged in, redirect to login page
        if (!response.ok) {
            console.warn("Auth check failed:", response.status);
            return NextResponse.redirect(new URL("/login", request.url));
        }

        return NextResponse.next();
    } catch (error) {
        // If an error occurs during checking if authenticated, redirect to login page
        console.error("Error checking auth:", error);
        if(request.nextUrl.pathname === "/login" || request.nextUrl.pathname === "/signup") {
            return NextResponse.next();
        }
        return NextResponse.redirect(new URL("/login", request.url));
    }
}

export const config = {
    matcher: [
        "/((?!_next/static|_next/image|favicon.ico|img/).*)",
    ],
};


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


