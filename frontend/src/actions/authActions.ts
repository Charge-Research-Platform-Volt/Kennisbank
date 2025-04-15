import { FormResponse } from "@/types/return.type";
import {RegisterRequestSchema, RegisterRequest} from "../types/registerRequest.type";
import { LoginRequest, LoginRequestSchema } from "@/types/loginRequest.type";

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

        const token = new URLSearchParams(window.location.search).get("token");

        if (!token) {
            return {
                success: false,
                message: "Invalid or missing token.",
            }
        }

        // Raw data from the form.
        const rawData: RegisterRequest = {
            // firstname: (formData.get("firstname") as string)?.trim(),
            // lastname: (formData.get("lastname") as string)?.trim(),
            email: (formData.get("email") as string)?.trim(),
            password: (formData.get("password") as string)?.trim(),
            token: (token as string)?.trim(),
        };

        // Validate the raw data, if it fails, return an error.
        const validatedData = RegisterRequestSchema.safeParse(rawData);
        
        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        // Send the data to the backend.
        const response : Response = await fetch("http://localhost:8080/Auth/signup", {
            method: "POST",
            body: JSON.stringify({
                email: rawData.email,
                password: rawData.password,
                token: rawData.token,
            }),
            headers: { "Content-Type": "application/json" },
        });

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {
            const data = await response.json();

            return {
                success: false,
                message: data.message || "An error occurred.",
                inputs: rawData,
            };
        }

        return {
            success: true,
            message: "Account created successfully.",
        };
    } catch (error) {
        console.error(`An error occurred: ${error}`);

        return {
            success: false,
            message: "An error occurred.",
        };
    }
};

export const Login = async (
    prevState: FormResponse<LoginRequest>,
    formData: FormData,
): Promise<FormResponse<LoginRequest>> => {
    try {
        console.log("Logging in...");

        // Raw data from the form.
        const rawData: LoginRequest = {
            email: (formData.get("email") as string)?.trim(),
            password: (formData.get("password") as string)?.trim(),
        };

        // Validate the raw data, if it fails, return an error.
        const validatedData = LoginRequestSchema.safeParse(rawData);
        
        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        // Send the data to the backend.
        const response = await fetch("http://localhost:8080/Auth/login?useCookies=true&useSessionCookies=true", {
            method: "POST",
            credentials: "include",
            body: JSON.stringify({
                email: rawData.email,
                password: rawData.password,
            }),
            headers: { "Content-Type": "application/json" },
        });

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {

            if(response.status === 401) {
                return {
                    success: false,
                    message: "Invalid email or password",
                    inputs: rawData,
                };
            }

            const data = await response.json();

            return {
                success: false,
                message: Object.values(data.errors).flat().join(" "),
                inputs: rawData,
            };
        }

        return {
            success: true,
            message: "Log in successful.",
        };
    } catch (error) {
        console.error(`An error occurred: ${error}`);

        return {
            success: false,
            message: "An error occurred.",
        };
    }
};

export async function Logout(): Promise<{success: boolean; message: string }>
{
    try
    {
        console.log("Logging out...");

        const response = await fetch(`http://localhost:8080/auth/logout`, {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({}),
        });

        if (!response.ok)
        {
            const data = await response.json();

            return {
                success: false,
                message: data.message || "Failed to log out",
            };
        }

        return {
            success: true,
            message: "Logged out successfully",
        }
    }
    catch (error)
    {
        console.error(`An error occurred during logout: ${error}`);
    
        return {
            success: false,
            message: "An error occurred during logout",
        };
    }
}
