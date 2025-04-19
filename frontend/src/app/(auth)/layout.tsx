import Image from "next/image";

export default async function LoginLayout({ children }: { children: React.ReactNode }) {
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
                
                {/* Copyright notice (bottom left) */}
                <label className="absolute left-4 bottom-4">
                    <i>©Utrecht University (ICS)</i>
                </label>
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


