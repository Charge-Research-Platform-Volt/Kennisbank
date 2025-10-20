import React from "react";
import { cookies } from "next/headers";

export default async function page() {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const response = await fetch(`${process.env.API_URL}/roles/current`, {
    method: "GET",
    headers: { Cookie: cookieHeader, "Content-Type": "application/json" },
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch user role: ${response.status}`);
  }

  const data = await response.json();
  console.log("User role:", data.role);

  return <div>page</div>;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


