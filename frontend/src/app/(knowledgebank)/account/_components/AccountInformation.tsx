'use client';

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button";
import { UpdateAccountDto } from "@/types/user.type";
import { useState } from "react";
import { UploadWithDto } from "@/actions/uploadActions";
import { toast } from "sonner";
import { Label } from "@radix-ui/react-label";
import { RevalidatePathFromClient } from "@/utils/revalidatePathFromClient";
import { useRouter } from "next/navigation";

export const AccountInformationFormSchema = z.object(
{
    firstName: z.string().min(1, { message: "First name is required" }),
    lastName: z.string().min(1, { message: "Last name is required" }),
    email: z.string().min(1, { message: "Email is required" }),
});


export default function AccountInformation({firstName, lastName, email}: {firstName: string; lastName: string; email: string}) {
    const [isLoading, setIsLoading] = useState(false);
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

                    {/* Change password button */}
                    <FormItem>  
                        <div className="flex gap-2 w-full mb-2 mt-2">
                            <FormLabel>Password</FormLabel>
                            <FormControl>
                                <Button type="button" className="ml-auto" onClick={() => router.push("account/change-password")} > Change Password </Button>
                            </FormControl>
                        </div>
                    </FormItem>

                    {/* Submit button */}
                    <Button type="submit" className="w-full" disabled={isLoading}>{isLoading ? "Saving..." : "Save"}</Button>
                </form>
            </Form>
        </div>
    );
}
