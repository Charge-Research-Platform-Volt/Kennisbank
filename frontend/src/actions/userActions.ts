"use server";

import { ApiResponse } from "@/types/apiResponse.type";
import { FormResponse } from "@/types/return.type";
import { SaveUserResponse, User, UserPageResponse, UsersArraySchema, UserSchema } from "@/types/user.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

export const DeleteUser = async (user: User ): Promise<FormResponse<User>> => {
    console.log("Deleting user: ", user.id);

    //validate the data
    const rawData = user;
    const validatedData = UserSchema.safeParse(rawData);
    if(!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
        };
    }

    // Send the data to the backend
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response : Response = await fetch(
        `${process.env.API_URL}/User/delete?userId=${encodeURIComponent(user.id)}`,
        {
            method: "DELETE",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );
    
    if (response.status == 403)
        return {success: false, message: "This action is forbidden."}

    // Check if the request was succesful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: await response.text(),
        };
    }
    
    //parse the data from the response
    const data = await response.json();

    // Revalidate the cache for the usertags page
    revalidatePath("/users");
    return {
        success: true,
        message: data.message,
    };
};

export const DeleteOwnAccount = async (): Promise<FormResponse<void>> => {
    console.log("Deleting account");

    // Send the data to the backend
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response : Response = await fetch(
        `${process.env.API_URL}/User/delete`,
        {
            method: "DELETE",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );
    
    if (response.status == 403)
        return {success: false, message: "This action is forbidden."}
    
    //parse the data from the response
    const data = await response.json();

    // Check if the request was succesful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: data.message,
        };
    }

    return {
        success: true,
        message: data.message,
    };
};

/**
 * Gets the current user Id
 *
 * @author Justin Liem
 * @returns Current user Id
 */
export const GetCurrentUserId = async (): Promise<ApiResponse> => {
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response : Response = await fetch(
        `${process.env.API_URL}/Auth/get-user-id`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );
    
    if (response.status == 403)
        return {success: false, message: "This action is forbidden."}

    // Check if the request was succesful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: "",
        };
    }

    //parse the data from the response
    const data = await response.json();
    
    return {
        success: true,
        message: "",
        body: data.body
    };
};

export const SaveUser = async (
    state: SaveUserResponse,
    { newEmail, newRole }: { newEmail: string; newRole: string }
): Promise<SaveUserResponse> => {
    console.log("Updating user: ", state.user.id);

    //validate the data
    const rawData = state.user;
    const validatedData = UserSchema.safeParse(rawData);
    if(!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            user: state.user,
        };
    }

    //create a new user object to avoid mutating the original one, this will be the result of the action
    const newUser = structuredClone(state.user);

    //update the email in the backend
    if(newEmail != state.user.email) {
        // Send the data to the backend
        const cookieHeader : ReadonlyRequestCookies = await cookies();
        const response : Response = await fetch(
            `${process.env.API_URL}/User/update-mail`,
            {
                method: "PATCH",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
                body: JSON.stringify({
                    userId: state.user.id,
                    email: newEmail,
                }),
            },
        );
        
        if (response.status == 403)
            return {success: false, message: "This action is forbidden.", user: newUser}
        
        //parse the data from the response
        const data = await response.json();

        // Check if the request was succesful, if not, return an error
        if (!response.ok) {
            return {
                success: false,
                message: "Saving failed: " + data.errors ? Object.values(data.errors).flat().join("\n") : data.message,
                user: newUser,
            };
        }

        //request was succesful, update the user object
        newUser.email = newEmail;
    }

    //update the role in the backend
    if(newRole != state.user.role) {
        // Send the data to the backend
        const cookieHeader : ReadonlyRequestCookies = await cookies();
        const response : Response = await fetch(
            `${process.env.API_URL}/Roles/assign`,
            {
                method: "PATCH",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
                body: JSON.stringify({
                    userId: state.user.id,
                    roleName: newRole,
                }),
            },
        );
        
        if (response.status == 403)
            return {success: false, message: "This action is forbidden.", user: newUser}

        //parse the data from the response
        const data = await response.json();

        // Check if the request was succesful, if not, return an error
        if (!response.ok) {
            return {
                success: false,
                message: "Saving role failed: " + data.message,
                user: newUser,
            };
        }

        //request was succesful, update the user object
        newUser.role = newRole;
    }

    // Revalidate the cache for the usertags page
    return {
        success: true,
        message: "Updating data succeeded",
        user: newUser,
    };
};

/**
 * 
 * @param pageIndex - The page index to fetch
 * @param query - The query to search for users
 * @returns - A promise that resolves to a UserPageResponse object containing the users and pagination information
 */
export const ListUsersPaged = async (pageIndex: number, query: string ): Promise<UserPageResponse> => {
    console.log("Getting user page: ");

    // Send the data to the backend
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response : Response = await fetch(
        `${process.env.API_URL}/User/list-paged?pageIndex=${pageIndex}&pageSize=50&searchQuery=${encodeURIComponent(query)}`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );

    //parse the data from the response
    const data = await response.json();

    // Check if the request was succesful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: data.message,
        };
    }

    //validate the data
    if(!data){
        return {
            success: false,
            message: "No data found",
        };
    }

    //validate the users array
    const users = data.body?.users ?? data.users;
    const validatedUsers = UsersArraySchema.safeParse(users);
    if (!validatedUsers.success) {
        return {
            success: false,
            message: validatedUsers.error.errors[0].message,
        };
    }

    revalidatePath("/users");
    
    // Check if the request was succesful, if not, return an error
    return {
        success: true,
        message: "Users fetched successfully",
        users: validatedUsers.data,
        pageIndex: data.pageIndex,
        pageSize: data.pageSize,
        pageCount: data.pageCount,
    }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


