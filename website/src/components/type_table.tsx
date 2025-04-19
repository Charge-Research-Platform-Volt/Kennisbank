import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"
import { CSharpType } from "@/components/csharp_type"

export type TypeItem = 
{
    name: string,
    description: string,
    type: string,
    links: string[],
}

export function TypeTable({ types }:{ types: TypeItem[] }) {
    const includeName = types.some((type) => type.name.trim() !== '');
    const includeDescription = types.some((type) => type.description.trim() !== '');

    let i = 0;
    
    return (
        <Table className="mt-1">
            <TableHeader>
                <TableRow>
                    { includeName? (<TableHead className="w-[100px]">Name</TableHead>) : "" }
                    <TableHead>Type</TableHead>
                    { includeDescription ? (<TableHead>Description</TableHead>) : "" }
                </TableRow>
            </TableHeader>
            <TableBody>
                { types.map((type) => (
                <TableRow key={i++}>
                    { includeName ? (<TableCell className="font-medium">{type.name}</TableCell>) : "" }
                    <TableCell>
                        <CSharpType type={type.type} links={type.links} />
                    </TableCell>
                    { includeDescription ? (<TableCell>{type.description}</TableCell>) : "" }
                </TableRow>
                ))}
            </TableBody>
        </Table>
    )
};