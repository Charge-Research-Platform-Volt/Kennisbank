import { cn } from "@/lib/utils";

interface HorizontalRuleProps {
  className?: string;
}

export function HorizontalRule({ className, ...props }: HorizontalRuleProps) {
  return <hr className={cn("my-6 border-b-stone-400", className)} {...props} />;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


