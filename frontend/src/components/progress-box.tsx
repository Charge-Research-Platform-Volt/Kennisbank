import { Progress } from "@/components/ui/progress";
import { Spinner } from "@/components/ui/spinner";

interface ProgressBoxProps
{
    title?: string;
    value: number;
    subtext?: string;
    currentStep?: number;
    maxSteps?: number;
}

export function ProgressBox({ title, value, subtext, currentStep, maxSteps }: ProgressBoxProps)
{
    return (
        <div className="p-1 w-full">
            <div className="bg-white border-2 border-gray-200 rounded-lg p-8">
                <div className="flex items-center justify-start mb-2">
                    <Spinner className="mr-2 text-gray-400" />
                    <h3 className="flex-1 text-base font-semibold text-gray-900">
                        {title} {currentStep && maxSteps && `(${currentStep}/${maxSteps})`}
                    </h3>
                </div>
                <Progress value={value} className="w-full" />
                <p className="text-sm text-gray-600 mt-1.5 pl-2">{subtext}</p>
            </div>
        </div>
    )
} 