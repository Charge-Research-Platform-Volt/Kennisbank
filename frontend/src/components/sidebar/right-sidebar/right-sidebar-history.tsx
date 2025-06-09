import { Button } from "@/components/ui/button";
import Divider from "../divider";
import { NewApiResponse } from "@/types/apiResponse.type";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";

interface Chat {
  id: string;
  userId: string;
  title: string;
  creationDate: string;
  // messages: any[];
}

type Chats = { [date: string]: Chat[] };

export default function RightSidebarHistory() {
  const route = useRouter();

  const GetAllChatsList = async (): Promise<Chats> => {
    try {
      const data = await fetch("/api/AI/all-chats", {
        method: "GET",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
      });

      const response: NewApiResponse<{ chats: Chats }> = await data.json();

      if (response.success) {
        return response.body.chats;
      }

      throw new Error(response.message || "Failed to fetch chats");
    } catch {
      toast.error("An error occurred while fetching the chat history.");
      return {};
    }
  };

  const { data: chats = {} } = useQuery<Chats>({
    queryKey: ["chats-history"],
    queryFn: GetAllChatsList,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  return (
    <div className="p-2">
      <Button variant="outline" className="" onClick={() => route.push("/chat")}>
        New Chat
      </Button>
      <Divider className="my-2" />

      <div className="mt-4 space-y-4">
        {Object.entries(chats).map(([date, chatList]) => (
          <div key={date} className="space-y-2">
            <h3 className="text-xs font-medium tracking-wide text-gray-500 uppercase">
              {new Date(date).toLocaleDateString("en-US", {
                weekday: "long",
                year: "numeric",
                month: "long",
                day: "numeric",
              })}
            </h3>
            <div className="space-y-1">
              {chatList.map((chat) => (
                <button key={chat.id} className="w-full rounded-md p-2 text-left text-sm text-gray-700 transition-colors duration-200 hover:bg-gray-100" onClick={() => route.push(`/chat/${chat.id}`)}>
                  <div className="truncate font-medium">{chat.title}</div>
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
