'use client';

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button";
import { UpdateAccountDto } from "@/types/user.type";
import { useEffect, useState } from "react";
import { UploadWithDto } from "@/actions/uploadActions";
import { toast } from "sonner";
import { RevalidatePathFromClient } from "@/utils/revalidatePathFromClient";
import { useRouter } from "next/navigation";
import Divider from "@/components/sidebar/divider";

export const AccountInformationFormSchema = z.object(
{
    firstName: z.string().min(1, { message: "First name is required" }),
    lastName: z.string().min(1, { message: "Last name is required" }),
    email: z.string().min(1, { message: "Email is required" }),
});

export const ChangePasswordFormSchema = z.object(
    {
        currentPassword: z.string().min(1, { message: "Current password is required" }),
        newPassword: z.string().min(1, { message: "New password is required" }),
        newPasswordRepeat: z.string().min(1, { message: "New password repeat is required" }),
    }).refine((data) => data.newPassword === data.newPasswordRepeat, {
        path: ["newPasswordRepeat"],
        message: "Passwords do not match",
    });

type ChangePasswordDto = z.infer<typeof ChangePasswordFormSchema>;

export default function AccountInformation({firstName, lastName, email}: {firstName: string; lastName: string; email: string}) {
    const [isLoading, setIsLoading] = useState(false);
    const [isLoadingPassword, setIsLoadingPassword] = useState(false);
    const router = useRouter();

    // Define the form
    const form = useForm<z.infer<typeof AccountInformationFormSchema>>({
        resolver: zodResolver(AccountInformationFormSchema),
        defaultValues: {
            firstName: firstName,
            lastName: lastName,
            email: email,
        },
    });

    // Function to be called when the form is submitted
    async function onSubmit(dto: UpdateAccountDto) 
    {
        setIsLoading(true);

        try
        {
            await UploadWithDto("/api/user/update", dto);
            toast.success("Account information updated successfully");
            RevalidatePathFromClient("/account");
        }
        catch (error)
        {
            console.error("Error updating account information:", error);
            if(error instanceof Error && !error.message.toLowerCase().includes("json"))
                toast.error(error.message);
            else if (typeof error === "string" && !error.toLowerCase().includes("json"))
                toast.error(error);
            else
                toast.error("Failed to update account information");
        }
        finally
        {
            setIsLoading(false);
        }
    };

    const formPassword = useForm<z.infer<typeof ChangePasswordFormSchema>>({
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
        if(!newPasswordRepeat) return;

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
    async function onSubmitPassword(dto: ChangePasswordDto) 
    {
        setIsLoadingPassword(true);

        try
        {
            await UploadWithDto("/api/auth/update-password", dto);
            toast.success("Password updated successfully");
        }
        catch (error)
        {
            console.error("Error updating password:", error);
            if(error instanceof Error && !error.message.toLowerCase().includes("json"))
                toast.error(error.message);
            else if (typeof error === "string" && !error.toLowerCase().includes("json"))
                toast.error(error);
            else
                toast.error("Failed to update password");
        }
        finally
        {
            setIsLoadingPassword(false);
        }
    };

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
            <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
                Account information
            </h1>
            
            <hr className="mb-4" />

            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-2">
                    {/* First name input */}
                    <FormField control={form.control} name="firstName" render={({ field }) => (
                        <FormItem>
                            <FormLabel>First name</FormLabel>
                            <FormControl>
                                <Input placeholder="First name" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Last name input */}
                    <FormField control={form.control} name="lastName" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Last name</FormLabel>
                            <FormControl>
                                <Input placeholder="Last name" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Email input */}
                    <FormField control={form.control} name="email" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Email</FormLabel>
                            <FormControl>
                                <Input placeholder="Email" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Submit button */}
                    <Button type="submit" className="w-full" disabled={isLoading}>{isLoading ? "Saving..." : "Save"}</Button>
                </form>
            </Form>
            <Divider className="my-8" />
        <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
            Change password
        </h1>
        
        <hr className="mb-4" />

            <Form {...formPassword}>
                <form onSubmit={formPassword.handleSubmit(onSubmitPassword)} className="flex flex-col gap-2">
                    {/* Current password input */}
                    <FormField control={formPassword.control} name="currentPassword" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Current password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Current password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* New password input */}
                    <FormField control={formPassword.control} name="newPassword" render={({ field }) => (
                        <FormItem>
                            <FormLabel>New password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Repeat password input */}
                    <FormField control={formPassword.control} name="newPasswordRepeat" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Repeat new password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Submit button */}
                    <Button type="submit" className="w-full" disabled={isLoadingPassword}>{isLoadingPassword ? "Saving..." : "Save"}</Button>
                </form>
            </Form>
        </div>
    );
}
