export interface ChatMessage {
  id: string;
  content: string;
  messageRole: "User" | "Assistant";
}

export type Messages = ChatMessage[];
