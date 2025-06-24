"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import React, { useState } from "react";

// For development purposes, you can enable the React Query Devtools to inspect queries
// import { ReactQueryDevtools } from "@tanstack/react-query-devtools";

export function TanStackQueryProvider({ children }: { children: React.ReactNode }) {
  // To ensure the QueryClient is only created once per component lifecycle
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            // With SSR, to set some default staleTime to avoid refetching immediately on the client
            staleTime: 60 * 1000, // 1 minute
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}

      {/* For development purposes, you can enable the React Query Devtools to inspect queries */}
      {/* <ReactQueryDevtools initialIsOpen={false} /> */}
    </QueryClientProvider>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


