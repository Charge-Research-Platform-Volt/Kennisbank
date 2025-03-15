export default async function Home() {
  const result = await FetchWithValidation(UsersArraySchema, "http://backend:8080/KnowledgeBank/members");

  return (
    <div>
      <h1>Hello World!</h1>
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
  );
}
