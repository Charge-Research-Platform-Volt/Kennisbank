import type { z } from "zod";

export async function FetchWithValidation<T>(
	schema: z.ZodSchema<T>,
	url: string,
): Promise<z.SafeParseReturnType<T, T>> {
	const response = await fetch(url);
	const data = await response.json();
	const result = schema.safeParse(data);
	return result;
}
