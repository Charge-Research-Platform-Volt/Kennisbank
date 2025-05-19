import { cn } from "@/lib/utils";

interface HorizontalRuleProps {
  className?: string;
}

export function HorizontalRule({ className, ...props }: HorizontalRuleProps) {
  return <hr className={cn("my-6 border-b-stone-400", className)} {...props} />;
}
