import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { OctagonAlert } from "lucide-react";
import { z } from "zod";
import AccountInformation from "./_components/AccountInformation";

export default async function AccountPage() {   
  const userEmail = await FetchWithValidation(z.object({ email: z.string() }), `${process.env.API_URL}/auth/ping`);
  const userFirstName = await FetchWithValidation(z.object({ firstName: z.string(), isAuthenticated: z.boolean() }), `${process.env.API_URL}/user/current-user-first-name`);
  const userLastName = await FetchWithValidation(z.object({ lastName: z.string(), isAuthenticated: z.boolean() }), `${process.env.API_URL}/user/current-user-last-name`);

  return (
    <div className="w-full">
          {
            !userEmail.success || !userFirstName.success || !userLastName.success ? (
              <div className="flex items-center gap-2">
                <OctagonAlert size={16} /> Failed to fetch user data
              </div>
            ) : (
              <AccountInformation firstName={userFirstName.data.firstName} lastName={userLastName.data.lastName} email={userEmail.data.email} />
            )
          }
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


