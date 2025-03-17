import Image from "next/image";
import { Inter } from "next/font/google";
import "../globals.css";
import { Toaster } from "sonner";

// Metadata
export const metadata: Metadata = {
    title: "KnowledgeBank",
    description: "Research knowledgebank by Charge",
  };
  
// Fonts
const inter = Inter({
    subsets: ["latin"],
    display: "swap",
});

export default function LoginLayout({ children }: { children: React.ReactNode }) {
    return (
        <html className={inter.className}>
            <body className="flex h-screen w-screen overflow-hidden">
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
            <Toaster />

        </body>
    </html>
    ); // set layout to null for login page (so no menu, etc.)
}