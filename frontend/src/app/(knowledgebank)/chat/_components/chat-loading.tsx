import React from "react";
import Skeleton from "react-loading-skeleton";
import "react-loading-skeleton/dist/skeleton.css";

export default function ChatLoading() {
  return (
    <div className="mx-auto w-full max-w-[800px] flex-1">
      <p className="text-center text-gray-500">Loading chat messages...</p>
      <Skeleton />
    </div>
  );
}
