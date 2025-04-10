"use client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Register } from "@/actions/authActions";
import { useState, useActionState, useEffect } from "react";
import { toast } from "sonner";
import { FormResponse } from "@/types/return.type";
import { RegisterRequest } from "@/types/registerRequest.type";
import { useRouter } from "next/navigation";

const initialState: FormResponse<RegisterRequest> = {
  success: false,
  message: "",
};

export default function SignUpPage() {
    const [firstName, setFirstName] = useState<string>("");
    const [lastName, setLastName] = useState<string>("");
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    const [retypePassword, setRetypePassword] = useState<string>("");
    const [termsBox, setTermsBox] = useState<boolean>(false);
  
    const [state, action, isPending] = useActionState(Register, initialState);

    const router = useRouter();

    useEffect(() => {
      if (state.success) {
        toast.success(state.message);
        router.push("/login");
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
                            <h1 className="font-bold text-3xl mb-1">Sign up</h1>
                            <p>Enter your personal data to create your account.</p>
                        </div>
                        <div className="flex gap-5 w-full">
                            <div className="w-full">
                                <label htmlFor="firstname" className="font-bold">FIRST NAME</label>
                                <Input value={firstName} type="text" placeholder="First name" name="firstname" onChange={(e) => setFirstName(e.target.value)} required/>
                            </div>
                            <div className="w-full">
                            <label htmlFor="lastname" className="font-bold">LAST NAME</label>
                                <Input value={lastName} type="text" placeholder="Last name" name="lastname" onChange={(e) => setLastName(e.target.value)} required/>
                            </div>
                        </div>
                        <div>
                            <label htmlFor="email" className="font-bold">EMAIL</label>
                            <Input value={email} type="email" placeholder="Email" name="email" onChange={(e) => setEmail(e.target.value.trim())} required/>
                        </div>
                        <div>
                            <label htmlFor="password" className="font-bold">PASSWORD</label>
                            <Input value={password} type="password" placeholder="Password" name="password" onChange={(e) => setPassword(e.target.value)} required/>
                        </div>
                        <div>
                            <label htmlFor="passwordcheck" className="font-bold">RETYPE PASSWORD</label>
                            <Input value={retypePassword} type="password" placeholder="Password" name="passwordcheck" onChange={(e) => setRetypePassword(e.target.value)} required/>
                            <div className="flex gap-2">
                                <input defaultChecked={termsBox} type="checkbox" name="terms" onChange={(e) => setTermsBox(e.target.checked)} required/>
                                <label htmlFor="terms" className="text-sm">I agree to the <a href="termsandconditions" className="text-blue-600 underline hover:text-blue-800">terms and conditions</a>.</label>
                            </div>
                        </div>
                        <Button type="submit" className="w-full" disabled={firstName.trim() == "" || lastName.trim() == "" || email == "" || password == "" || retypePassword == "" || !termsBox || isPending || state.success}>{isPending || state.success ? "Signing up..." : "Sign up"}</Button>
                    </form>
                </div>
            </div>
        </div>
        
    );
  }