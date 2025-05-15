export interface ChatMessage {
  id: string;
  message: string;
  sender: "user" | "system";
}

export type Messages = ChatMessage[];
