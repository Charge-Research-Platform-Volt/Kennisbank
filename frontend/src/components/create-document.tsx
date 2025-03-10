"use client";

import React, { useActionState, useEffect } from "react";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import type { DocumentBase } from "@/types/document.type";
import type { FormResponse } from "@/types/return.type";
import { AddDocument } from "@/actions/documentActions";
import { Log } from "../../Pino";
import { toast } from "sonner";

const initialState: FormResponse<DocumentBase> = {
    success: false,
    message: "",
};

export default function CreateDocument() {
    /*
        const [state, action, isPending] = useActionState(
        AddDocument,
        initialState,
    );

    useEffect(() => {
        if (state.success) {
            toast.success(state.message);
        } else if (state.message) {
            toast.error(state.message);
        }
    }, [state]);
    */ //NOT COMPATIBLE WITH NEW CODE
    return (
        {/* 
        <form className="mb-4 flex max-w-xl space-x-2" action={action}>
            <Input
                type="text"
                name="name"
                placeholder="Document name"
                disabled={isPending}
                defaultValue={state.inputs?.name}
            />
            <Input
                type="text"
                name="description"
                placeholder="Document description"
                disabled={isPending}
                defaultValue={state.inputs?.description}
            />
            <Button
                className="w-24"
                variant="default"
                type="submit"
                disabled={isPending}
            >
                {isPending ? "Creating..." : "Create"}
            </Button>
        </form>*/} //NOT COMPATIBLE WITH NEW CODE
    );
}
