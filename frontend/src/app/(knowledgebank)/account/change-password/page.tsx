'use client';

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { UploadWithDto } from "@/actions/uploadActions";
import { toast } from "sonner";

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

export default function AccountInformation() {
    const [isLoading, setIsLoading] = useState(false);

    // Define the form
    const form = useForm<z.infer<typeof ChangePasswordFormSchema>>({
        resolver: zodResolver(ChangePasswordFormSchema),
        defaultValues: {
            currentPassword: "",
            newPassword: "",
            newPasswordRepeat: "",
        },
    });

    // Function to update the password match error if the new password changes
    const newPassword = form.watch("newPassword");
    const newPasswordRepeat = form.watch("newPasswordRepeat");
    useEffect(() => {
        if(!newPasswordRepeat) return;

        if (newPassword !== newPasswordRepeat) {
        form.setError("newPasswordRepeat", {
            type: "manual",
            message: "Passwords do not match",
        });
        } else {
        form.clearErrors("newPasswordRepeat");
        }
    }, [newPassword, newPasswordRepeat, form]);

    // Function to be called when the form is submitted
    async function onSubmit(dto: ChangePasswordDto) 
    {
        setIsLoading(true);

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
            setIsLoading(false);
        }
    };

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
            <Button variant="link" className="mb-4 p-0 ml-0" onClick={() => window.history.back()}>
                <span className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300">
                    &#x25c0; Back
                </span>
            </Button>
            
            <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
                Change password
            </h1>
            
            <hr className="mb-4" />

            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-2">
                    {/* Current password input */}
                    <FormField control={form.control} name="currentPassword" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Current password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Current password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* New password input */}
                    <FormField control={form.control} name="newPassword" render={({ field }) => (
                        <FormItem>
                            <FormLabel>New password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Repeat password input */}
                    <FormField control={form.control} name="newPasswordRepeat" render={({ field }) => (
                        <FormItem>
                            <FormLabel>Repeat new password</FormLabel>
                            <FormControl>
                                <Input type="password" placeholder="Password" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />

                    {/* Submit button */}
                    <Button type="submit" className="w-full" disabled={isLoading}>{isLoading ? "Saving..." : "Save"}</Button>
                </form>
            </Form>
        </div>
    );
}
