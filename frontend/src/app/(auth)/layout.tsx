import Image from "next/image";
import { redirect } from "next/navigation";
import { cookies } from "next/headers";

export default async function LoginLayout({ children }: { children: React.ReactNode }) {
    const cookieHeader = await cookies();
    
    const response = await fetch("http://backend:8080/api/auth/status", {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
            Cookie: cookieHeader.toString() || "",
        },
      });
    
    const data = await response.json();

    if (data.isAuthenticated) {
    redirect("/");
    }
    
    return (
        <div className="flex w-full h-screen">
            {/* Login/signup part (left part) */}
            <div className="w-7/18 flex flex-col justify-center items-center p-4">
                {/* Logo */}
                <Image
                    src="/img/Charge-logo-NL-purple.png"
                    width={142}
                    height={56.49}
                    alt="logo"
                    className="absolute top-4 left-4"
                />

                {children}
            </div>

            {/* Image part (right part) */}
            <div className="w-11/18 flex justify-end ml-auto mr-0 rounded p-2">
                <Image 
                    src="/img/login-image.jpeg" 
                    width={0} 
                    height={0}
                    sizes="100vw"
                    alt="image"
                    className="h-full w-auto object-cover rounded-2xl"
                />
            </div>
        </div>
    ); 
}