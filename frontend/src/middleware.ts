import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export async function middleware(request: NextRequest) {
    try {
        // Check in backend if logged in
        const response = await fetch(`${process.env.API_URL}/Auth/ping`, {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                Cookie: request.headers.get("cookie") || "", // Cookies meesturen!
            },
        });

        // If endpoint is /login or /signup, don't redirect if not logged in, redirect if logged in
        const pathname = request.nextUrl.pathname;
        if (pathname.startsWith("/login") || pathname.startsWith("/signup")) {
            if (!response.ok) {
                console.warn("Auth check failed:", response.status);
                return NextResponse.next();
            }

            const redirect = request.nextUrl.searchParams.get("redirect");
            if(redirect)
            {
                return NextResponse.redirect(new URL(redirect, request.url)); 
            }
                    
            return NextResponse.redirect(new URL("/", request.url));
        }

        // One exception, so the client can call the api to create cookies
        if (pathname.startsWith("/api/Auth/")) { return NextResponse.next() }

        // If not logged in, redirect to login page
        if (!response.ok) {
            console.warn("Auth check failed:", response.status);
            const redirectUrl = new URL("/login", request.url);
            redirectUrl.searchParams.set("redirect", request.nextUrl.pathname);
            return NextResponse.redirect(redirectUrl);
        }

        return NextResponse.next();
    } catch (error) {
        // If an error occurs during checking if authenticated, redirect to login page
        console.error("Error checking auth:", error);
        if(request.nextUrl.pathname.startsWith("/login") || request.nextUrl.pathname.startsWith("/signup")) {
            return NextResponse.next();
        }
        const redirectUrl = new URL("/login", request.url);
        redirectUrl.searchParams.set("redirect", request.nextUrl.pathname);
        return NextResponse.redirect(redirectUrl);
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


