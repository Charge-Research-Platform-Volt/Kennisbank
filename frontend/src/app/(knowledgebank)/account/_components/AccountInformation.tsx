"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { UserData } from "@/types/user.type";
import { useEffect, useState } from "react";
import { UploadWithDto } from "@/actions/uploadActions";
import { toast } from "sonner";
import { RevalidatePathFromClient } from "@/utils/revalidatePathFromClient";
import Divider from "@/components/sidebar/divider";
import DeleteAccountConfirmationDialog from "./DeleteAccountConfirmationDialog";
import EditableAvatar from "@/components/ui/editable-avatar";
import { usePathname, useRouter } from "next/navigation";
import { LoaderCircle } from "lucide-react";

export default function AccountInformation({ userData }: { userData: UserData }) {
  const avatarUrl = userData.customAvatarVersion ? `/api/user/current/avatar?v=${userData.customAvatarVersion}` : "/img/default-profile-picture.svg";
  const [isLoadingAvatar, setIsLoadingAvatar] = useState(false);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [isLoadingPassword, setIsLoadingPassword] = useState(false);
  const [deleteAccountConfirmationDialogOpen, setDeleteAccountConfirmationDialogOpen] = useState(false);

  const router = useRouter();
  const pathName = usePathname();

  const ChangeInfoFormSchema = z.object({
    newFirstName: z.string(),
    newLastName: z.string(),
    newEmail: z.string(),
  });

  type ChangeInfoForm = z.infer<typeof ChangeInfoFormSchema>;

  const ChangePasswordFormSchema = z
    .object({
      currentPassword: z.string().min(1, { message: "Current password is required" }),
      newPassword: z.string().min(1, { message: "New password is required" }),
      newPasswordRepeat: z.string().min(1, { message: "New password repeat is required" }),
    })
    .refine((data) => data.newPassword === data.newPasswordRepeat, {
      path: ["newPasswordRepeat"],
      message: "Passwords do not match",
    });

  type ChangePasswordDto = z.infer<typeof ChangePasswordFormSchema>;

  // Define the form
  const form = useForm<ChangeInfoForm>({
    resolver: zodResolver(ChangeInfoFormSchema),
    defaultValues: {
      newFirstName: "",
      newLastName: "",
      newEmail: "",
    },
  });

  // Function to be called when the form is submitted
  async function onSubmitDetails(dto: ChangeInfoForm) {
    setIsLoadingDetails(true);

    try {
      await UploadWithDto("/api/user/update-details", dto);
      toast.success("Account information updated successfully");
      RevalidatePathFromClient("/account");
    } catch (error) {
      console.error("Error updating account information:", error);
      if (error instanceof Error && !error.message.toLowerCase().includes("json")) toast.error(error.message);
      else if (typeof error === "string" && !error.toLowerCase().includes("json")) toast.error(error);
      else toast.error("Failed to update account information");
    } finally {
      setIsLoadingDetails(false);
    }
  }

  const formPassword = useForm<ChangePasswordDto>({
    resolver: zodResolver(ChangePasswordFormSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      newPasswordRepeat: "",
    },
  });

  // Function to update the password match error if the new password changes
  const newPassword = formPassword.watch("newPassword");
  const newPasswordRepeat = formPassword.watch("newPasswordRepeat");
  useEffect(() => {
    if (!newPasswordRepeat) return;

    if (newPassword !== newPasswordRepeat) {
      formPassword.setError("newPasswordRepeat", {
        type: "manual",
        message: "Passwords do not match",
      });
    } else {
      formPassword.clearErrors("newPasswordRepeat");
    }
  }, [newPassword, newPasswordRepeat, formPassword]);

  // Function to be called when the form is submitted
  const onSubmitPassword = async (dto: ChangePasswordDto) => {
    setIsLoadingPassword(true);

    try {
      await UploadWithDto("/api/auth/update-password", dto);
      toast.success("Password updated successfully");
    } catch (error) {
      console.error("Error updating password:", error);
      if (error instanceof Error && !error.message.toLowerCase().includes("json")) toast.error(error.message);
      else if (typeof error === "string" && !error.toLowerCase().includes("json")) toast.error(error);
      else toast.error("Failed to update password");
    } finally {
      setIsLoadingPassword(false);
    }
  };

  const updateAvatar = async (body: FormData | null) => {
    setIsLoadingAvatar(true);

    const response = await fetch("api/user/update-avatar", {
      method: "PATCH",
      body,
    });

    if (response.ok) {
      toast.success(response.text());
      await RevalidatePathFromClient("/account");
      router.push(pathName);
      router.refresh();
    } else {
      toast.error(response.text());
      setIsLoadingAvatar(false);
    }
  };

  const handleDefault = () => {
    updateAvatar(null);
  };

  const handleConfirm = (blob: Blob) => {
    const formData = new FormData();
    formData.append("newAvatar", blob);
    updateAvatar(formData);
  };

  useEffect(() => {
    setIsLoadingAvatar(true);
    const img = new Image();
    img.src = avatarUrl;
    img.onload = async () => {
      setIsLoadingAvatar(false);
    };
    img.onerror = () => {
      toast.error("Failed to load avatar.");
      setIsLoadingAvatar(false);
    };
  }, [avatarUrl]);

  return (
    <div className="container mx-auto mt-5 max-w-7xl px-4 sm:px-6 lg:px-8">
      <h1 className="mb-2 text-2xl tracking-tight text-gray-900 md:text-3xl lg:text-4xl dark:text-gray-100">Account information</h1>

      <hr className="mb-4" />

      {isLoadingAvatar ? (
        <div className="flex h-32 items-center justify-center">
          <LoaderCircle className="h-8 w-8 animate-spin text-gray-500" />
        </div>
      ) : (
        <EditableAvatar url={avatarUrl} onConfirm={handleConfirm} onDefault={handleDefault} />
      )}

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmitDetails)} className="flex flex-col gap-2">
          {/* First name input */}
          <FormField
            control={form.control}
            name="newFirstName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>First name</FormLabel>
                <FormControl>
                  <Input placeholder={userData.firstName} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Last name input */}
          <FormField
            control={form.control}
            name="newLastName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Last name</FormLabel>
                <FormControl>
                  <Input placeholder={userData.lastName} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Email input */}
          <FormField
            control={form.control}
            name="newEmail"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input placeholder={userData.email} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Submit button */}
          <Button type="submit" className="w-full" disabled={isLoadingDetails}>
            {isLoadingDetails ? "Saving..." : "Save"}
          </Button>
        </form>
      </Form>
      <Divider className="my-8" />
      <h1 className="mb-2 text-2xl tracking-tight text-gray-900 md:text-3xl lg:text-4xl dark:text-gray-100">Change password</h1>

      <hr className="mb-4" />

      <Form {...formPassword}>
        <form onSubmit={formPassword.handleSubmit(onSubmitPassword)} className="flex flex-col gap-2">
          {/* Current password input */}
          <FormField
            control={formPassword.control}
            name="currentPassword"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Current password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Current password" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* New password input */}
          <FormField
            control={formPassword.control}
            name="newPassword"
            render={({ field }) => (
              <FormItem>
                <FormLabel>New password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Password" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Repeat password input */}
          <FormField
            control={formPassword.control}
            name="newPasswordRepeat"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Repeat new password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Password" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Submit button */}
          <Button type="submit" className="w-full" disabled={isLoadingPassword}>
            {isLoadingPassword ? "Saving..." : "Save"}
          </Button>
        </form>
      </Form>

      <Divider className="my-8" />
      <h1 className="mb-2 text-2xl tracking-tight text-gray-900 md:text-3xl lg:text-4xl dark:text-gray-100">Delete account</h1>

      <hr className="mb-4" />

      <DeleteAccountConfirmationDialog open={deleteAccountConfirmationDialogOpen} onOpenChange={setDeleteAccountConfirmationDialogOpen} />

      <div className="flex">
        <p className="text-red-500">Deleting your account is permanent and cannot be undone. Please proceed with caution.</p>
        <Button className="ml-auto bg-red-500 hover:bg-red-400" onClick={() => setDeleteAccountConfirmationDialogOpen(true)}>
          Delete account
        </Button>
      </div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


