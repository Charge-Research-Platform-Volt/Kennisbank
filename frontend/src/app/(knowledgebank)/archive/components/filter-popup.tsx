"use client";

export default function FilterPopup({ isVisible, className, ref }: { isVisible: boolean, className?: string, ref?: React.Ref<HTMLDivElement> }) {
    return (
        <>
            {isVisible && (
                <div ref={ref} className={`z-50 flex bg-white p-2 border-2 border-gray-100 rounded-sm ${className}`}>
                    <p>Filters</p>
                </div>
            )}
        </>        
    )
}
