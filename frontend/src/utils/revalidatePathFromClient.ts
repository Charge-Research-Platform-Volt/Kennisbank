"use server";

import { revalidatePath } from "next/cache";

export const RevalidatePathFromClient = async (path: string) => {
    revalidatePath(path);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


