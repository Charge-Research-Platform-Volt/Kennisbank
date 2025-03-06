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
    );

    return (
        <div className="flex">
            <div className="flex-1 p6 p-4">
                Hello World!
                <div>
                    <h1>Team (from the backend):</h1>

                    {result.data &&
                        result.data.map((member) => (
                            <div key={member.name}>
                                <h2>{member.name}</h2>
                            </div>
                        ))}
                </div>
                <br />
                <hr />
                <ListDocuments />
            </div>
        </div>
    );
}
