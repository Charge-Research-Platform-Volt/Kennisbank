import { FormResponse } from "@/types/return.type";
import { InviteRequestSchema, InviteRequest } from "@/types/inviteRequest.type";

export const Invite = async (
    prevState: FormResponse<InviteRequest>,
    formData: FormData,
): Promise<FormResponse<InviteRequest>> => {
    try {
        console.log("Inviting user...");
        const rawData: InviteRequest = {
            email: (formData.get("email") as string)?.trim(),
        };

        // Validate the raw data
        const validatedData = InviteRequestSchema.safeParse(rawData);

        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        // Send the data to the backend
        const response : Response = await fetch(`/api/Auth/invite`, {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(rawData.email),
        });

        // Check if the request was successful
        if (!response.ok) {
            const data = await response.json();

            return {
                success: false,
                message: data.message || "Failed to send invitation",
                inputs: rawData,
            };
        }

        return {
            success: true,
            message: "Invitation sent successfully.",
        };
    } catch (error) {
        console.error(`An error occurred while sending invitation: ${error}`);

        return {
            success: false,
            message: "An error occurred while sending the invitation.",
        };
    }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


