import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export async function middleware(request: NextRequest) {
    try {
        
        
        // Fetch met headers van de request
        const response = await fetch("http://backend:8080/api/auth/status", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                Cookie: request.headers.get("cookie") || "", // Cookies meesturen!
            },
        });

        const pathname = request.nextUrl.pathname;
        console.log("Pathname:", pathname);

        if (pathname === "/login" || pathname === "/signup") {
            if (!response.ok) {
                console.warn("Auth check failed:", response.status);
                return NextResponse.next();
            }
    
            const data = await response.json();
    
            if (data.isAuthenticated) {
                return NextResponse.redirect(new URL("/", request.url));
            }
            
            return NextResponse.next();
        }

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
        if(request.nextUrl.pathname === "/login" || request.nextUrl.pathname === "/signup") {
            return NextResponse.next();
        }
        return NextResponse.redirect(new URL("/login", request.url));
    }
}

export const config = {
    matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
