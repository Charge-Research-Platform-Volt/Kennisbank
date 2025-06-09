"use client";

import React, { useCallback } from "react";
import { Button } from "../ui/button";
import { useSearchParams, useRouter } from "next/navigation";
import { List, Table } from "lucide-react";

export default function LayoutSwitch({ isListLayout }: { isListLayout: boolean }) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const toggleLayout = useCallback(() => {
    const params = new URLSearchParams(searchParams.toString());

    if (isListLayout) {
      // Switch to table view (remove the parameter)
      params.delete("layout");
    } else {
      // Switch to list view
      params.set("layout", "list");
    }

    router.push(`?${params.toString()}`);
  }, [isListLayout, router, searchParams]);

  return (
    <div className="flex h-10 items-center rounded-lg border p-1">
      <Button variant={isListLayout ? "ghost" : "default"} size="sm" onClick={toggleLayout} className="h-full gap-0 rounded-sm has-[>svg]:pl-1">
        <Table className="mr-2 h-4 w-4" />
        Table
      </Button>
      <Button variant={isListLayout ? "default" : "ghost"} size="sm" onClick={toggleLayout} className="h-full gap-0 rounded-sm has-[>svg]:pl-2">
        <List className="mr-2 h-4 w-4" />
        List
      </Button>
    </div>
  );
}
