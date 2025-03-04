import { z } from "zod";

const UserSchema = z.object({
  name: z.string(),
});

const UsersArraySchema = z.array(UserSchema);

export default async function Home() {
  const response = await fetch("http://backend:8080/KnowledgeBank/members");
  const data = await response.json();
  const result = UsersArraySchema.safeParse(data);

  if (!result.success) {
    throw new Error("Data validation failed");
  }

  return (
    <div className="h-full">
      Hello World!
      <div>
        <h1>Team:</h1>

        {result.data.map((member) => (
          <div key={member.name}>
            <h2>{member.name}</h2>
          </div>
        ))}
      </div>
    </div>
  );
}
