"use client"

interface OrganisationContentProps
{
    id: string;
}

export function OrganisationContent({ id }: OrganisationContentProps) 
{
    return (
        <>
            <h1>Organisation</h1>
            <p>{id}</p>
        </>
    )
}