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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


