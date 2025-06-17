"use client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Register } from "@/actions/authActions";
import { useState, useActionState, useEffect, startTransition } from "react";
import { toast } from "sonner";
import { FormResponse } from "@/types/return.type";
import { RegisterRequest } from "@/types/registerRequest.type";
import { useRouter } from "next/navigation";
import EditableAvatar from "@/components/ui/editable-avatar";

const initialState: FormResponse<RegisterRequest> = {
  success: false,
  message: "",
};

type AvatarState = { url: string; blob: Blob | null };

export default function SignUpPage() {
  const [firstName, setFirstName] = useState<string>("");
  const [lastName, setLastName] = useState<string>("");
  const [email, setEmail] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [retypePassword, setRetypePassword] = useState<string>("");
  const [avatar, setAvatar] = useState<AvatarState>({ url: "/img/default-profile-picture.svg", blob: null });
  const [termsBox, setTermsBox] = useState<boolean>(false);

  const [state, action, isPending] = useActionState(Register, initialState);

  const router = useRouter();

  const revokeBlobUrl = () => {
    if (avatar.blob) URL.revokeObjectURL(avatar.url);
  };

  // Make sure that the avatars last url is revoked once the component unmounts.
  useEffect(() => {
    return revokeBlobUrl;
  }, []);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
      router.push("/login");
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);

    if (avatar.blob) formData.append("avatar", avatar.blob);

    startTransition(() => {
      action(formData);
    });
  };

  const handleChange = (blob: Blob | null) => {
    revokeBlobUrl();
    setAvatar({ url: blob ? URL.createObjectURL(blob) : "/img/default-profile-picture.svg", blob });
  };

  return (
    <div className="flex h-full w-full">
      {/* Form */}
      <div className="flex h-full w-full items-center justify-center">
        <div className="mx-auto w-3/4 p-4">
          <form className="flex flex-col gap-6" onSubmit={handleSubmit}>
            <div>
              <h1 className="mb-1 text-3xl font-bold">Sign up</h1>
              <p>Enter your personal data to create your account.</p>
            </div>
            <div className="flex w-full gap-5">
              <div className="w-full">
                <label htmlFor="firstname" className="font-bold">
                  FIRST NAME
                </label>
                <Input value={firstName} type="text" placeholder="First name" name="firstname" onChange={(e) => setFirstName(e.target.value)} required />
              </div>
              <div className="w-full">
                <label htmlFor="lastname" className="font-bold">
                  LAST NAME
                </label>
                <Input value={lastName} type="text" placeholder="Last name" name="lastname" onChange={(e) => setLastName(e.target.value)} required />
              </div>
            </div>
            <div>
              <label htmlFor="email" className="font-bold">
                EMAIL
              </label>
              <Input value={email} type="email" placeholder="Email" name="email" onChange={(e) => setEmail(e.target.value.trim())} required />
            </div>
            <div>
              <label htmlFor="password" className="font-bold">
                PASSWORD
              </label>
              <Input value={password} type="password" placeholder="Password" name="password" onChange={(e) => setPassword(e.target.value)} required />
            </div>
            <div>
              <EditableAvatar url={avatar.url} onConfirm={handleChange} onDefault={() => handleChange(null)} />
            </div>
            <div>
              <label htmlFor="passwordcheck" className="font-bold">
                RETYPE PASSWORD
              </label>
              <Input value={retypePassword} type="password" placeholder="Password" name="passwordcheck" onChange={(e) => setRetypePassword(e.target.value)} required />
              <div className="flex gap-2">
                <input defaultChecked={termsBox} type="checkbox" name="terms" onChange={(e) => setTermsBox(e.target.checked)} required />
                <label htmlFor="terms" className="text-sm">
                  I agree to the{" "}
                  <a href="termsandconditions" className="text-blue-600 underline hover:text-blue-800">
                    terms and conditions
                  </a>
                  .
                </label>
              </div>
            </div>
            <Button
              type="submit"
              className="w-full"
              disabled={firstName.trim() == "" || lastName.trim() == "" || email == "" || password == "" || retypePassword == "" || !termsBox || isPending || state.success}
            >
              {isPending || state.success ? "Signing up..." : "Sign up"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
