import { Table, TableCaption, TableHeader, TableRow, TableHead, TableBody, TableCell } from "@/components/ui/table";
import { ListItem } from "./BadgeList";
import Skeleton from "react-loading-skeleton";
import { useState } from "react";
import Divider from "../divider";
import OpenFileButton from "@/components/open-file-button";
import GetDownloadIcon from "@/components/getDownloadIcon";

export default function ResourceList({resources, header} : {resources: ListItem[] | null; header: string}) {
    const [length, setLength] = useState<number>(5);



    function addRows () {
        let r = resources as ListItem[];
        if (length + 5 > r.length) {
            setLength(r.length)
        }
        else {
        setLength(length+5)}
    }

    function hideRows () {
        setLength(5)}


    return (
        <>
        <div className="w-full flex items-center gap-2">
            <span>{header}</span>
                <div className="flex-1 h-px bg-gray-300" />
        </div>
        {resources && resources.length === 0 ? (
            <div className=" text-gray-500 text-center">No resources available</div>
        ) : (
        <Table className="bg-gray-200 rounded-2xl table-fixed">
            {!resources ? (<TableCaption><Skeleton /></TableCaption>) : (<>{resources.length === length ? (
            <TableCaption className={`hover:underline cursor-pointer`} onClick={() => hideRows()}>Hide</TableCaption>) : (
            <TableCaption className={`hover:underline cursor-pointer`} onClick={() => addRows()}>Load more</TableCaption>)}</>)}
            <TableBody>
                {resources ? ( resources.slice(0, length).map((item: ListItem) => (
                    <TableRow key={item.id || item.name} className="cursor-pointer">
                        <TableCell className="overflow-hidden text-ellipsis w-[93%]"><>{item.name}</></TableCell>
                        <TableCell className=" w-[7%]"><GetDownloadIcon fileType={item.type} className="h-5 w-5" /></TableCell>
                    </TableRow>
                ))
                ) : (
                    <>
                    <TableRow><TableCell><Skeleton /></TableCell></TableRow>
                    <TableRow><TableCell><Skeleton /></TableCell></TableRow>
                    <TableRow><TableCell><Skeleton /></TableCell></TableRow>
                    <TableRow><TableCell><Skeleton /></TableCell></TableRow>
                    <TableRow><TableCell><Skeleton /></TableCell></TableRow>
                    </>
                )}
            </TableBody>
        </Table>
        )}
        </>
    );
}