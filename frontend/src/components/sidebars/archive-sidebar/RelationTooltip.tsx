"use client"

import React from "react";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useDebouncedCallback } from "use-debounce";

interface RelationTooltipProps
{
    item: {
        id: string;
        name: string;
        relation: string | undefined;
    };
    entityType: "resources" | "persons" | "organisations";
    currentId: string;
    relationType: string;
    editMode: boolean;
    onClick?: () => void;
    onRelationUpdate: (itemId: string, newRelation: string) => void;
    trashComponent?: React.ReactNode;
}

export function RelationTooltip({
    item,
    entityType,
    currentId,
    relationType,
    editMode,
    onClick,
    onRelationUpdate,
    trashComponent
}: RelationTooltipProps)
{
    const [localRelation, setLocalRelation] = React.useState<string>(item.relation || "");

    // Update local state when item changes
    React.useEffect(() => {
        setLocalRelation(item.relation || "");
    }, [item.relation]);

    // Debounced API call to update relation
    const debouncedUpdate = useDebouncedCallback(async (newValue: string) => {
        try {
            const response = await fetch(
                `/api/${entityType}/${currentId}/relations/update-role/${relationType}/${item.id}?newRole=${encodeURIComponent(newValue)}`,
                {
                    method: 'PATCH',
                    credentials: 'include'
                }
            );

            if (response.ok) {
                onRelationUpdate(item.id, newValue);
            }
        } catch (error) {
            console.error("Error updating relation:", error);
        }
    }, 500);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newValue = e.target.value;
        setLocalRelation(newValue);
        debouncedUpdate(newValue);
    };

    // Determine if we should reverse the relationship direction
    const isReversed = (entityType === "persons" && relationType === "related-resources") ||
                     (entityType === "organisations" && relationType === "related-resources");

    // Only show tooltip if there's a relation or if in edit mode
    // For non-reversed relationships, we need a relation to show the tooltip
    // For reversed relationships, we can show tooltip even without relation (optional)
    const shouldShowTooltip = editMode || (item.relation && !isReversed) || isReversed;

    if (!shouldShowTooltip) {
        return (
            <Badge
                variant="outline"
                className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer"
                onClick={onClick}
            >
                <span className="truncate">{item.name}</span>
                {editMode && trashComponent}
            </Badge>
        );
    }

    return (
        <Tooltip key={item.id} delayDuration={500}>
            <TooltipTrigger asChild>
                <Badge
                    variant="outline"
                    className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer"
                    onClick={onClick}
                >
                    <span className="truncate">{item.name}</span>
                    {editMode && trashComponent}
                </Badge>
            </TooltipTrigger>
            <TooltipContent className="bg-popover text-popover-foreground border border-border [&_svg]:!hidden">
                {!editMode && (() => {
                    // Determine if we should reverse the relationship direction
                    const isReversedRel = (entityType === "persons" && relationType === "related-resources") ||
                                         (entityType === "organisations" && relationType === "related-resources");

                    const entityTypeLabel = entityType === "resources" ? "resource" :
                                          entityType === "persons" ? "person" : "organisation";

                    // For reversed relationships, always show tooltip even without relation
                    if (isReversedRel) {
                        if (item.relation) {
                            return (
                                <p className="text-sm">
                                    This {entityTypeLabel} is{" "}
                                    <span className="italic">{item.relation}</span> of{" "}
                                    <span className="font-semibold">{item.name}</span>
                                </p>
                            );
                        } else {
                            return (
                                <p className="text-sm">
                                    This {entityTypeLabel} is related to{" "}
                                    <span className="font-semibold">{item.name}</span>
                                </p>
                            );
                        }
                    } else if (item.relation) {
                        return (
                            <p className="text-sm">
                                <span className="font-semibold">{item.name}</span> is{" "}
                                <span className="italic">{item.relation}</span> of this {entityTypeLabel}
                            </p>
                        );
                    }
                    return null;
                })()}

                {editMode && (() => {
                    // Determine if we should reverse the relationship direction
                    const isReversed = (entityType === "persons" && relationType === "related-resources") ||
                                     (entityType === "organisations" && relationType === "related-resources");

                    const entityTypeLabel = entityType === "resources" ? "resource" :
                                          entityType === "persons" ? "person" : "organisation";

                    if (isReversed) {
                        return (
                            <div className="flex flex-col gap-1 min-w-[250px]">
                                <p className="text-sm">This {entityTypeLabel} is</p>
                                <Input
                                    type="text"
                                    placeholder="e.g., contributor, affiliated..."
                                    value={localRelation}
                                    onChange={handleInputChange}
                                    className="text-sm"
                                    onClick={(e) => e.stopPropagation()}
                                />
                                <p className="text-sm">of <span className="font-semibold">{item.name}</span></p>
                            </div>
                        );
                    } else {
                        return (
                            <div className="flex flex-col gap-1 min-w-[250px]">
                                <p className="text-sm"><span className="font-semibold">{item.name}</span> is</p>
                                <Input
                                    type="text"
                                    placeholder="e.g., publisher, funder..."
                                    value={localRelation}
                                    onChange={handleInputChange}
                                    className="text-sm"
                                    onClick={(e) => e.stopPropagation()}
                                />
                                <p className="text-sm">of this {entityTypeLabel}</p>
                            </div>
                        );
                    }
                })()}
            </TooltipContent>
        </Tooltip>
    );
}
