"use server";

import { revalidatePath } from "next/cache";

export const RevalidatePathFromClient = async (path: string) => {
    revalidatePath(path);
}