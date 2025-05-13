"use client"

interface PersonContentProps 
{
    id: string;
}

export function PersonContent({ id }: PersonContentProps) 
{
    return (
        <>
            <h1>Person</h1>
            <p>{id}</p>
        </>
    )
}