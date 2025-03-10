import ListDocuments from "@/components/list-documents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { z } from "zod";

const UserSchema = z.object({
    name: z.string(),
});

const UsersArraySchema = z.array(UserSchema);

export default async function Home() {
    const result = await FetchWithValidation(
        UsersArraySchema,
        "http://backend:8080/KnowledgeBank/members",
    );   //DUE TO BACKEND CODE CHANGES THIS CODE ISNT COMPATIBLE
            //ANYMORE AND WAS CAUSING CONTINUOUS RECOMPILATION OF FRONTEND

    return (
        <div className="flex">
            <div className="flex-1 p6 p-4">
            </div>
        </div>
    );
}
