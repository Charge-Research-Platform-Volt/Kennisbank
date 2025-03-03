"use server";

import type { DocumentBase } from "@/types/document.type";
import type { FormResponse, ReturnType } from "@/types/return.type";
import { revalidatePath } from "next/cache";

export const AddDocument = async (
	prevState: FormResponse<DocumentBase>,
	formData: FormData,
): Promise<ReturnType> => {
	const name = formData.get("name") as string;
	if (!name) {return { success: false, message: "Name is required" }}

	const description = formData.get("description") as string;
	if (!description) {return { success: false, message: "Description is required" }}

	const response = await fetch("http://backend:8080/Drive/add-document", {
		method: "POST",
		body: JSON.stringify({
			name: name,
			description: description,
		}),
		headers: { "Content-Type": "application/json" },
	});

	if (!response.ok) {
		return {
			success: false,
			message: "Failed to create document",
		};
	}

	revalidatePath("/");
	return {
		success: true,
		message: "Document created",
	};
};
