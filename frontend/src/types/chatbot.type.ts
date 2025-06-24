export interface ChatMessage {
  id: string;
  content: string;
  messageRole: "User" | "Assistant";
}

export type Messages = ChatMessage[];


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


