interface NewPersonProps 
{
    onCreate?: (id: string) => void
}

export default function NewPerson({ onCreate }: NewPersonProps) 
{
    return (
        <h1>New person!</h1>
    );
}