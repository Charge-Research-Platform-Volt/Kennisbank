export interface ChatMessage {
  id: string;
  content: string;
  messageRole: "User" | "System";
}

export type Messages = ChatMessage[];
