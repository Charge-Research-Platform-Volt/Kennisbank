"use client";

import { Login } from "@/actions/authActions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LoginRequest } from "@/types/loginRequest.type";
import { FormResponse } from "@/types/return.type";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, useActionState, useEffect, Suspense } from "react";
import { toast } from "sonner";

const initialState: FormResponse<LoginRequest> = {
  success: false,
  message: "",
};

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirect = searchParams.get("redirect");

  const [email, setEmail] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [state, action, isPending] = useActionState(Login, initialState);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
      router.push(redirect || "/");
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state, redirect, router]);

  return (
    <div className="flex h-full w-full">
      {/* Form */}
      <div className="flex h-full w-full items-center justify-center">
        <div className="mx-auto w-3/4 p-4">
          <form className="flex flex-col gap-6" action={action}>
            <div>
              <h1 className="mb-1 text-3xl font-bold">Sign in</h1>
              <p>Welcome back, please enter your details to sign in.</p>
            </div>
            <div>
              <label htmlFor="email" className="font-bold">
                EMAIL
              </label>
              <Input 
                value={email} 
                type="text" 
                placeholder="Email" 
                name="email" 
                id="email" 
                onChange={(e) => setEmail(e.target.value.trim())} 
                required 
              />
            </div>
            <div>
              <label htmlFor="password" className="font-bold">
                PASSWORD
              </label>
              <Input 
                value={password} 
                type="password" 
                placeholder="Password" 
                name="password" 
                id="password" 
                onChange={(e) => setPassword(e.target.value.trim())} 
                required 
              />
            </div>
            <Button 
              type="submit" 
              className="w-full" 
              disabled={password === "" || email === "" || state.success || isPending}
            >
              {isPending || state.success ? "Signing in..." : "Sign in"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}

function LoginFallback() {
  return (
    <div className="flex h-full w-full items-center justify-center">
      <div className="mx-auto w-3/4 p-4">
        <div className="flex flex-col gap-6">
          <div>
            <h1 className="mb-1 text-3xl font-bold">Sign in</h1>
            <p>Loading...</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={<LoginFallback />}>
      <LoginForm />
    </Suspense>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)