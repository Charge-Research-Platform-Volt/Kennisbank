import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export default function LoginPage() {
    return (
        <div className="flex w-full h-full">
            {/* Form */}
            <div className="flex justify-center items-center h-full w-full">
                <div className="w-3/4 mx-auto p-4">
                    <h1 className="font-bold text-3xl mb-1">Sign in</h1>
                    <p>Welcome back, please enter your details to sign in.</p>
                    <form className="flex flex-col gap-10 mt-10">
                        <div>
                            <label htmlFor="email" className="font-bold">EMAIL</label>
                            <Input type="text" placeholder="Email" name="emal" />
                        </div>
                        <div>
                            <label htmlFor="password" className="font-bold">PASSWORD</label>
                            <Input type="password" placeholder="Password" name="password" />
                        </div>
                        <Button type="submit" className="w-full">Sign in</Button>
                    </form>
                </div>
            </div>
        </div>
        
    );
  }