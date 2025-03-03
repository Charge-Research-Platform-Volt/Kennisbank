"use server";

import { DocumentBaseSchema, type DocumentBase } from "@/types/document.type";
import type { FormResponse } from "@/types/return.type";
import { revalidatePath } from "next/cache";

export const AddDocument = async (
	prevState: FormResponse<DocumentBase>,
	formData: FormData,
): Promise<FormResponse<DocumentBase>> => {
	try {
		// Raw data from the form.
		const rawData: DocumentBase = {
			name: formData.get("name") as string,
			description: formData.get("description") as string,
		};

		// Validate the raw data, if it fails, return an error.
		const validatedData = DocumentBaseSchema.safeParse(rawData);

		if (!validatedData.success) {
			return {
				success: false,
				message: validatedData.error.errors[0].message,
				inputs: rawData,
			};
		}

		// Send the data to the backend.
		const response = await fetch("http://backend:8080/Drive/add-document", {
			method: "POST",
			body: JSON.stringify({
				name: rawData.name,
				description: rawData.description,
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
		revalidatePath("/");

		return {
			success: true,
			message: data.message,
		};
	} catch (error) {
		return {
			success: false,
			message: "An error occurred.",
		};
	}
};
