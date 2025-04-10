import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { DocumentPageResponseSchema } from "@/types/document.type";
import Link from "next/link";
import Greeting from "../components/greating-text";
import Archive from "@/icons/archive";
import Projects from "@/icons/projects-icon";
import Search from "@/icons/search-icon";
import GetFileIcon from "@/components/getFileIcon";
import OpenFileButton from "@/components/open-file-button";

export default async function Home() {
  // fetches all documents
  const result = await FetchWithValidation(DocumentPageResponseSchema, "http://backend:8080/Storage/list-paged?pageIndex=1&pageSize=4");

  let files = result.data?.files ?? [];

  // only display first 4 files, needs to be updated to display recently opened files
  if (files.length > 4) {
    files = files.slice(0, 4);
  }

  return (
    <div className="flex min-h-full flex-col items-center justify-center px-6">
      <div className="w-full max-w-4xl text-center">
        <Greeting />
        <p className="mb-10 text-lg text-gray-500">Where do you want to go?</p>

        {/* Main buttons */}
        <div className="mb-10 flex justify-center gap-10">
          {[
            { icon: <Search className="h-50 w-50" fill="#4b5563" />, text: "Search", path: "" },
            { icon: <Projects className="h-50 w-50" fill="#4b5563" />, text: "Projects", path: "/projects" },
            { icon: <Archive className="h-50 w-50" fill="#4b5563" />, text: "Archive", path: "/archive" },
          ].map((btn, index) => (
            <Link key={index} href={btn.path}>
              <div className="flex h-32 w-32 cursor-pointer flex-col items-center rounded-xl border border-gray-300 bg-gray-100 p-6 shadow-md transition hover:bg-gray-200">
                {btn.icon}
                <p className="mt-2 text-lg font-medium text-gray-600">{btn.text}</p>
              </div>
            </Link>
          ))}
        </div>

        {/* Files */}
        <div className="mx-auto w-full max-w-xl rounded-lg bg-gray-100 shadow-lg">
          <h2 className="mt-1 mb-1 text-xl font-semibold">Files</h2>
          <div className="flex flex-col">
            {files.map((file, index) => (
              <div
                key={file.id}
                className={`flex items-center justify-between gap-2 border bg-white p-2 hover:bg-gray-200 ${index === 0 ? "rounded-t-lg" : ""} ${index === files.length - 1 ? "rounded-b-lg" : ""} ${index !== 0 ? "border-t-0" : ""} `}
              >
                <span className="flex w-full items-center gap-2 overflow-hidden text-lg">
                  <GetFileIcon fileType={file.fileType} />
                  <span className="block w-[250px] truncate text-left text-sm md:w-[350px] lg:w-[450px]">{file.name}</span>
                </span>

                <OpenFileButton file={file} />
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
