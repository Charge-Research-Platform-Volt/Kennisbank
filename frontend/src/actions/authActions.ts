import {RegisterRequestScheme, RegisterRequest} from "../types/registerRequest.type";

export const Register = async (
    prevState: FormResponse<RegisterRequest>,
    formData: FormData,
): Promise<FormResponse<RegisterRequest>> => {
    try {
        console.log("Registering user...");
        if(formData.get("password") !== formData.get("passwordcheck")) {
            return {
                success: false,
                message: "Passwords do not match",
            };
        }

        if(!formData.get("terms")) {
            return {
                success: false,
                message: "You must agree to the terms and conditions",
            };
        }

        // Raw data from the form.
        const rawData: RegisterRequest = {
            // firstname: (formData.get("firstname") as string)?.trim(),
            // lastname: (formData.get("lastname") as string)?.trim(),
            email: (formData.get("email") as string)?.trim(),
            password: (formData.get("password") as string)?.trim(),
        };

        // Validate the raw data, if it fails, return an error.
        const validatedData = RegisterRequestScheme.safeParse(rawData);
        
        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        // Send the data to the backend.
        const response = await fetch("http://backend:8080/register", {
            method: "POST",
            body: JSON.stringify({
                email: rawData.email,
                password: rawData.password,
            }),
            headers: { "Content-Type": "application/json" },
        });
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {
            return {
                success: false,
                message: data.message,
                inputs: rawData,
            };
        }

        // Revalidate the cache for the home page.
        revalidatePath("/auth/signup");

        return {
            success: true,
            message: data.message,
        };
    } catch (error) {
        Log.error(`An error occurred: ${error}`);

        return {
            success: false,
            message: "An error occurred.",
        };
    }
};
