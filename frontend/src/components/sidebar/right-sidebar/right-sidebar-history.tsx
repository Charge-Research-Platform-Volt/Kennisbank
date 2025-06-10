import Divider from "../divider";
import { NewApiResponse } from "@/types/apiResponse.type";
import { toast } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Trash2 } from "lucide-react";

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
  const pathname = usePathname();
  const queryClient = useQueryClient();

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

  const { data: chats = {}, refetch } = useQuery<Chats>({
    queryKey: ["chats-history"],
    queryFn: GetAllChatsList,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const handleDeleteChat = async (chatId: string) => {
    try {
      const response = await fetch(`/api/AI/delete-chat/${chatId}`, {
        method: "DELETE",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
      });
      const result: NewApiResponse<{ success: boolean }> = await response.json();
      if (result.success) {
        refetch();
        toast.success("Chat deleted successfully.");
      } else {
        toast.error(result.message || "Failed to delete chat.");
      }
    } catch {
      toast.error("An error occurred while deleting the chat.");
    }
  };

  return (
    <div className="p-2">
      <Divider className="my-2 mt-11" />

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
                <div
                  key={chat.id}
                  className={`${pathname === `/chat/${chat.id}` ? "bg-gray-100" : "bg-gray-50"} flex w-full cursor-pointer items-center justify-between rounded-md p-1 pl-2 transition-colors duration-200 hover:bg-gray-100`}
                  onClick={() => {
                    if (pathname !== `/chat/${chat.id}`) {
                      queryClient.invalidateQueries({ queryKey: ["chat-messages", chat.id] });
                      route.push(`/chat/${chat.id}`);
                    }
                  }}
                  title={chat.title}
                >
                  <div className="min-w-0 flex-1 text-left text-sm text-gray-700">
                    <div className="truncate pr-2 font-medium">{chat.title}</div>
                  </div>

                  <button
                    className="flex-shrink-0 rounded-md bg-gray-50 p-2 text-gray-400 transition-all duration-200 hover:text-red-500"
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      handleDeleteChat(chat.id);

                      if (pathname === `/chat/${chat.id}`) {
                        route.push("/chat");
                      }
                    }}
                    title="Delete chat"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {Object.keys(chats).length === 0 && <div className="text-center text-sm text-gray-500">No chat history available.</div>}
    </div>
  );
}
