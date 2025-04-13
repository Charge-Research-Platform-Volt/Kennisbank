"use client";

import { handleOpenFile } from "@/actions/openFileActions";
import { Button } from "@/components/ui/button";
import DownloadIcon from "@/icons/download-icon";
import { ResourceResponse } from "@/types/resource.type";

export default function OpenFileButton({file, asIcon = false}: {file: ResourceResponse, asIcon?: boolean}) {
    return asIcon ? 
    (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={() => handleOpenFile(file)}
          title="Download file"
        >
          <DownloadIcon className="h-5 w-5" fill="#737373" />
        </Button>

    ) : 
    (
        <Button onClick={() => handleOpenFile(file)} className="w-[99.08px]">{file.fileType === "pdf" ? 'Open' : 'Download' }</Button>
    );
}
