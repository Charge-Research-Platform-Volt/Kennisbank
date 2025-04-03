"use client";

import { Button } from "@/components/ui/button";
import { useTransition } from "react";
import { toast } from "sonner";
import { ReactElement } from "react";

// Define the expected structure of an action result
interface ActionResult {
  success?: boolean;
  message?: string;
}

// Props for the ActionButton component
interface ActionButtonProps<T> {
  // The action function that returns a Promise with ActionResult
  action: (arg: T) => Promise<ActionResult>;
  // Name of the action for logging
  actionName: string;
  // Argument to pass to the action function
  actionArg: T;
  // Message to show on success
  successMessage?: string;
  // Icon component to render
  icon: React.ComponentType<any>;
  // Title for the button
  title: string;
  // Optional className for styling
  className?: string;
  // Props to pass to the icon component
  iconProps?: Record<string, any>;
}

/**
 * ActionButton - A reusable button component for handling async actions with loading state and toast notifications
 * 
 * This component encapsulates common patterns used in action buttons throughout the application:
 * - Manages loading state during async operations using React's useTransition
 * - Handles action execution and provides consistent console logging
 * - Displays toast notifications for success and error states
 * - Renders a button with an icon and consistent styling
 * 
 * @template T - The type of the argument passed to the action function
 * 
 * @example
 * // Button that approves a user tag
 * <ActionButton<string>
 *   action={ApproveUserTag}
 *   actionName="Approving user tag"
 *   actionArg={tag.id}
 *   successMessage="Tag approved"
 *   icon={ApproveTagIcon}
 *   title="Approve tag"
 * />
 * 
 * @returns A styled button that executes the provided action when clicked
 */export default function ActionButton<T>({
  action,
  actionName,
  actionArg,
  successMessage = "Action completed",
  icon: Icon,
  title,
  className = "bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground",
  iconProps = { className: "h-5 w-5", fill: "#737373" }
}: ActionButtonProps<T>): ReactElement {
  const [isPending, startTransition] = useTransition();

  function handleAction() {
    console.log(`${actionName}:`, (actionArg as any)?.name);
    startTransition(async () => {
      const result = await action(actionArg);
      if (result && result.success) {
        toast.success(successMessage);
      } else if (result && result.message) {
        toast.error(result.message);
      } else {
        toast.error(`Error ${actionName.toLowerCase()}`);
      }
    });
  }

  return (
    <Button
      className={className}
      variant="default"
      type="button"
      onClick={handleAction}
      disabled={isPending}
      title={title}
    >
      <Icon {...iconProps} />
    </Button>
  );
}