interface ResetFilterProps 
{
    onClick?: () => void;
}

export default function ResetFilter({onClick}: ResetFilterProps) 
{
    return (
        <div className="w-full flex justify-center items-center">
            <span className="text-gray-500 cursor-pointer" onClick={onClick}>Reset</span>
        </div>
    )
}