import { Table, TableCaption, TableHeader, TableRow, TableHead, TableBody, TableCell } from "@/components/ui/table";
import { ListItem } from "./BadgeList";
import Skeleton from "react-loading-skeleton";
import { useEffect, useState } from "react";
import Divider from "../divider";
import OpenFileButton from "@/components/open-file-button";
import GetDownloadIcon from "@/components/getDownloadIcon";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";

export default function ResourceList({resources, header} : {resources: ListItem[] | null; header: string}) {
    const [length, setLength] = useState<number>(5);
    const [maxLength, setMaxLength] = useState<boolean>(false);
    const { navigate } = useSidebar();

    useEffect(() =>{
        if (resources) {
            if (resources.length > 5) {setLength(5); setMaxLength(false)}
            else { setLength(resources.length); setMaxLength(true);}
        }
    }, [resources])


    function addRows () {
        let r = resources as ListItem[];
        if (length + 5 >= r.length) {
            setLength(r.length);
            setMaxLength(true);
        }
        else {
        setLength(length+5)}
    }

    function hideRows () {
        setLength(5); setMaxLength(false)}

    function navigateTo(id: string) {
        navigate(id, MetadataTypeEnum.RESOURCE);
    }


    return (
        <>
        <div className="w-full flex items-center gap-2 select-none">
            <span>{header}</span>
                <div className="flex-1 h-px bg-gray-300" />
        </div>
        {resources && resources.length === 0 ? (
            <div className=" text-gray-500 text-center select-none">No resources available</div>
        ) : (
        <Table className="bg-gray-200 rounded-2xl table-fixed select-none">
            {!resources || (maxLength && length < 6) ? (<TableCaption></TableCaption>) : (<>{maxLength ? (
            <TableCaption className={`hover:underline cursor-pointer select-none`} onClick={() => hideRows()}>Hide</TableCaption>) : (
            <TableCaption className={`hover:underline cursor-pointer select-none`} onClick={() => addRows()}>Load more</TableCaption>)}</>)}
            <TableBody className="select-none">
                {resources ? ( resources.slice(0, length).map((item: ListItem) => (
                    <TableRow key={item.id || item.name} onClick={() => navigateTo(item.id)} className="cursor-pointer select-none">
                        <TableCell className="select-none overflow-hidden text-ellipsis w-[93%]"><>{item.name}</></TableCell>
                        <TableCell className="select-none w-[7%]"><GetDownloadIcon fileType={item.type} className="h-5 w-5" /></TableCell>
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