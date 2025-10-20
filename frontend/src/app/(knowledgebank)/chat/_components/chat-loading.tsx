import React from "react";
import Skeleton from "react-loading-skeleton";
import "react-loading-skeleton/dist/skeleton.css";

export default function ChatLoading() {
  return (
    <div className="mx-auto w-full max-w-[800px] flex-1">
      <div className="fade-in-animaiton mt-12 flex w-full justify-end">
        <div className="bg-sidebar w-full max-w-[700px] rounded-md border p-5 font-medium">
          <Skeleton count={3} width="100%" height={20} />
        </div>
      </div>
      <div className="fade-in-animaiton mt-12 flex w-full justify-start">
        <div className={`w-full max-w-[700px]`}>
          <Skeleton count={5} width="100%" height={20} />
        </div>
      </div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


