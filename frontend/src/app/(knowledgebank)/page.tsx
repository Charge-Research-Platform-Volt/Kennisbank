import { Button } from "@/components/ui/button";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { DocumentPageResponseSchema } from "@/types/document.type";
import Image from "next/image";
import Link from "next/link";
import Greeting from "./components/greating-text";

export default async function Home() {
  const result = await FetchWithValidation(
      DocumentPageResponseSchema,
      "http://backend:8080/Storage/list-all",
  );

  let files = result.data?.files ?? [];

  if(files.length > 4){
    files = files.slice(0, 4);
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen px-6">
      <div className="max-w-4xl w-full text-center">
        <Greeting />
        <p className="text-gray-500 mb-10 text-lg">Where do you want to go?</p>

        {/* Main buttons */}
        <div className="flex gap-10 mb-10 justify-center">
          {[
            { icon: "/img/search-icon-homepage.svg", text: "Search", path: "" },
            { icon: "/img/projects-icon-homepage.svg", text: "Projects", path: "/projects"},
            { icon: "/img/archive-icon-homepage.svg", text: "Archive", path: "/archive" },
          ].map((btn, index) => (
            <Link key={index} href={btn.path}>
              <div
                key={index}
                className="flex flex-col items-center bg-gray-100 p-6 rounded-xl border border-gray-300 hover:bg-gray-200 transition cursor-pointer w-32 h-32 shadow-md"
              >
                <Image src={btn.icon} alt={btn.text} width={50} height={50} />
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
                <span className="flex items-center gap-4 text-lg">
                  📄 {file.name}
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
