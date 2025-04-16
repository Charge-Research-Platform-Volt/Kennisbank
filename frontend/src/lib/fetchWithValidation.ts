import type { z } from "zod";
//import { Log } from "../../Pino";
import { cookies } from "next/headers";

/**
 * Fetches data from a specified URL and validates it against a provided Zod schema.
 *
 * @template T - The type of data to be validated.
 * @param schema - The Zod schema used to validate the fetched data.
 * @param url - The URL to fetch data from.
 * @returns A promise that resolves to a Zod safe parse result containing either the validated data or validation errors.
 *
 * @remarks
 * If the fetch request fails (response.ok is false), the function will return an error object
 * with the message from the backend response. The backend should ensure that error messages
 * don't contain sensitive information.
 */

export async function FetchWithValidation<T>(schema: z.ZodSchema<T>, url: string): Promise<z.SafeParseReturnType<T, T>> {
  try {
    // Fetch the data from the backend
    const cookieHeader = await cookies();
    const response = await fetch(url, {
      method: "GET",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        Cookie: cookieHeader.toString() || "",
      },
    });

    // Parse the response as JSON
    const data = await response.json();

    // Check if the response is OK, if not, return an error got from the backend. (make sure the backend returns an error object and does not contain any sensitive information)
    if (!response.ok) {
      const fetchErrorResponse = {
        success: false,
        error: new Error(data.message),
      } as z.SafeParseReturnType<T, T>;
      return fetchErrorResponse;
    }

    // Validate the data with the schema
    const result = schema.safeParse(data);
    return result;
  } catch (error) {
    console.log(error);
    //Log.error(`An error occurred: ${error}`);

    // Return an error object if an exception occurs
    const fetchErrorResponse = {
      success: false,
      error: new Error("An error occurred."),
    } as z.SafeParseReturnType<T, T>;
    return fetchErrorResponse;
  }
}
