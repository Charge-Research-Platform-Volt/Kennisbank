"use client";

import { useActionState, useEffect, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Mail } from "lucide-react";
import { Invite } from "@/actions/adminActions";
import { toast } from "sonner";
import { FormResponse } from "@/types/return.type";
import { LoginRequest } from "@/types/loginRequest.type";

const initialState: FormResponse<LoginRequest> = {
  success: false,
  message: "",
};

export default function InvitationCard() {
  const [state, formAction] = useActionState(Invite, initialState); 
  const [isPendingTransition, startTransition] = useTransition();

  useEffect(() => {
    if (state.success) {
      toast.success(state.message || "Invitation sent successfully");
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    startTransition(async () => {
      const formData = new FormData(event.currentTarget);
      await formAction(formData);
    });
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-full px-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-center">
            <div className="flex items-center justify-center mb-4">
              <Mail className="w-12 h-12 text-gray-600 mr-2" />
              <span>Invite New User</span>
            </div>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <div className="space-y-4">
              <Input 
                type="email"
                name="email"
                placeholder="Enter email address"
                defaultValue={state.inputs?.email || ''}
                className="w-full"
              />
              <Button 
                type="submit" 
                className="w-full"
                disabled={isPendingTransition}
              >
                {isPendingTransition ? "Sending Invitation..." : "Send Invitation"}
              </Button>
            </div>
          </form>
          <p className="text-xs text-gray-500 mt-4 text-center">
            An email with an invitation code will be sent to the provided email address.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}