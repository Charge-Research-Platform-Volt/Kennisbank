import ListDocuments from "@/components/list-documents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { z } from "zod";

const UserSchema = z.object({
    name: z.string(),
});

const UsersArraySchema = z.array(UserSchema);

export default async function Home() {

    return (
        <div className="flex">
            <div className="flex-1 p6 p-4">
            </div>
        </div>
    );
}
