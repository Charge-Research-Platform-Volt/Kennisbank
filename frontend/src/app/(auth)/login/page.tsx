"use client";

import { Login } from "@/actions/authActions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LoginRequest } from "@/types/loginRequest.type";
import { FormResponse } from "@/types/return.type";
import { useRouter } from "next/navigation";
import { useState, useActionState, useEffect } from "react";
import { toast } from "sonner";

const initialState: FormResponse<LoginRequest> = {
    success: false,
    message: "",
  };

export default function LoginPage() {
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    
    const [state, action, isPending] = useActionState(Login, initialState);
    
    const router = useRouter();
    useEffect(() => {
        if (state.success) {
          toast.success(state.message);
          router.push("/");
        } else if (state.message) {
          toast.error(state.message);
        }
      }, [state]);

    return (
        <div className="flex w-full h-full">
            {/* Form */}
            <div className="flex justify-center items-center h-full w-full">
                <div className="w-3/4 mx-auto p-4">
                    <form className="flex flex-col gap-6" action={action}>
                        <div>
                            <h1 className="font-bold text-3xl mb-1">Sign in</h1>
                            <p>Welcome back, please enter your details to sign in.</p>
                        </div>
                        <div>
                            <label htmlFor="email" className="font-bold">EMAIL</label>
                            <Input value={email} type="text" placeholder="Email" name="email" onChange={(e) => setEmail(e.target.value.trim())} required/>
                        </div>
                        <div>
                            <label htmlFor="password" className="font-bold">PASSWORD</label>
                            <Input value={password} type="password" placeholder="Password" name="password" onChange={(e) => setPassword(e.target.value.trim())} required/>
                        </div>
                        <Button type="submit" className="w-full" disabled={password == "" || email == "" || state.success || isPending}>{isPending || state.success ? "Signing in..." : "Sign in"}</Button>
                    </form>
                </div>
            </div>
        </div>
        
    );
  }