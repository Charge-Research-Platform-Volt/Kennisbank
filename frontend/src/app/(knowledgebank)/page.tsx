import { Button } from "@/components/ui/button";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ResourcePageResponseSchema } from "@/types/resource.type";
import Link from "next/link";
import Greeting from "../components/greating-text";
import Archive from "@/icons/archive";
import Projects from "@/icons/projects-icon";
import Search from "@/icons/search-icon";
import GetFileIcon from "@/components/getFileIcon";

export default async function Home() {
  // fetches all documents
  const result = await FetchWithValidation(
      ResourcePageResponseSchema,
      "http://backend:8080/Storage/list-paged?pageIndex=1&pageSize=4",
  );

  let files = result.data?.files ?? [];

  // only display first 4 files, needs to be updated to display recently opened files
  if(files.length > 4){
    files = files.slice(0, 4);
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-full px-6">
      <div className="max-w-4xl w-full text-center">
        <Greeting />
        <p className="text-gray-500 mb-10 text-lg">Where do you want to go?</p>

        {/* Main buttons */}
        <div className="flex gap-10 mb-10 justify-center">
          {[
            { icon: <Search className="w-50 h-50" fill="#4b5563" />, text: "Search", path: "" },
            { icon: <Projects className="w-50 h-50" fill="#4b5563" />, text: "Projects", path: "/projects"},
            { icon: <Archive className="w-50 h-50" fill="#4b5563" />, text: "Archive", path: "/archive" },
          ].map((btn, index) => (
            <Link key={index} href={btn.path}>
              <div
                key={index}
                className="flex flex-col items-center bg-gray-100 p-6 rounded-xl border border-gray-300 hover:bg-gray-200 transition cursor-pointer w-32 h-32 shadow-md"
              >
                {btn.icon}
                <p className="text-gray-600 font-medium text-lg mt-2">{btn.text}</p>
              </div>
            </Link>
          ))}
        </div>

        {/* Files */}
        <div className="w-full max-w-xl bg-gray-100  rounded-lg shadow-lg mx-auto">
          <h2 className="font-semibold text-xl mb-1 mt-1">Files</h2>
          <div className="flex flex-col">
            {files.map((file, index) => (
              <div
                key={file.id}
                className={`flex items-center justify-between p-2 bg-white border hover:bg-gray-200
                  ${index === 0 ? "rounded-t-lg" : ""} 
                  ${index === files.length - 1 ? "rounded-b-lg" : ""}
                  ${index !== 0 && index !== files.length - 1 ? "border-t-0" : ""}
                `}
              >
                <span className="flex items-center gap-2 text-lg w-full overflow-hidden">
                  {GetFileIcon(file.fileType)}
                  <span className="truncate w-[250px] md:w-[350px] lg:w-[450px] block text-left text-sm">
                    {file.name}
                  </span>
                </span>
                <Link href={`file/${file.id}`}><Button>Open</Button></Link>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
