import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { OctagonAlert } from "lucide-react";
import AccountInformation from "./_components/AccountInformation";
import { UserDataSchema } from "@/types/user.type";

export default async function AccountPage() {   
  const userFetch = await FetchWithValidation(UserDataSchema, `${process.env.API_URL}/user/current/account`);
  
  return (
    <div className="w-full">
          {
            userFetch.success ? (
              <AccountInformation userData={userFetch.data!} />
            ) : (
              <div className="flex items-center gap-2">
                <OctagonAlert size={16} /> Failed to fetch user data
              </div>
            )
          }
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


