import React, { useState, useRef, forwardRef, ChangeEvent, DragEvent } from 'react';

// Define props interface for the component
interface FileInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  className?: string;
  buttonText?: string;
  placeholder?: string;
  variant?: 'default' | 'outline' | 'ghost' | 'primary';
  onChange?: (e: ChangeEvent<HTMLInputElement>) => void;
}

/**
 * @summary File input component with drag and drop support
 */
const FileInput = forwardRef<HTMLInputElement, FileInputProps>(({ 
  className,
  buttonText = "Choose file",
  placeholder = "No file selected",
  variant = "default",
  onChange,
  ...props
}, ref) => {
  const [fileName, setFileName] = useState<string>("");
  const [isDragging, setIsDragging] = useState<boolean>(false);
  const innerRef = useRef<HTMLInputElement>(null);
  
  // Combine the forwarded ref with our internal ref
  const handleInputRef = (node: HTMLInputElement | null) => {
    // Save a reference to the node internally
    if (node) {
      innerRef.current = node;
    }
    
    // Handle different types of refs
    if (typeof ref === 'function') {
      ref(node);
    } else if (ref) {
      ref.current = node;
    }
  };

  const handleClick = (): void => {
    innerRef.current?.click();
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement>): void => {
    const files = e.target.files;
    if (files && files.length > 0) {
      setFileName(files[0].name);
    } else {
      setFileName("");
    }
    if (onChange) {
      onChange(e);
    }
  };

  const handleDragEnter = (e: DragEvent<HTMLDivElement>): void => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>): void => {
    e.preventDefault();
    e.stopPropagation();
    if (!isDragging) {
      setIsDragging(true);
    }
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>): void => {
    e.preventDefault();
    e.stopPropagation();
    
    // Check if the drag leaves the component entirely, not just its children
    if (e.currentTarget.contains(e.relatedTarget as Node)) {
      return; // Don't set isDragging to false if we're still within the component
    }
    
    setIsDragging(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>): void => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      // Update the file input
      if (innerRef.current) {
        // Create a new FileList-like object
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(e.dataTransfer.files[0]);
        innerRef.current.files = dataTransfer.files;
        
        // Trigger change event
        const changeEvent = new Event("change", { bubbles: true });
        innerRef.current.dispatchEvent(changeEvent);
        
        setFileName(e.dataTransfer.files[0].name);
        
        // Call the onChange handler if provided
        if (onChange && innerRef.current.files) {
          const syntheticEvent = {
            target: {
              files: innerRef.current.files
            }
          } as React.ChangeEvent<HTMLInputElement>;
          onChange(syntheticEvent);
        }
      }
    }
  };

  return (
    <div 
      data-state={isDragging ? "dragging" : undefined}
      data-slot="input"
      aria-invalid={props['aria-invalid']}
      className={`
        flex items-center h-9 w-full rounded-md border transition-colors cursor-pointer overflow-hidden
        ${isDragging ? 'border-black border-2' : 'border-input hover:border-input/80'}
        focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]
        aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive
        ${className || ''}
      `}
      onClick={handleClick}
      onDragEnter={handleDragEnter}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <div className="flex-1 px-3 py-1 overflow-hidden whitespace-nowrap text-ellipsis text-sm">
        {fileName || placeholder}
      </div>
      
      <div className="flex items-center justify-center px-3 h-full bg-secondary text-secondary-foreground">
        <span className="text-sm font-medium">{buttonText}</span>
      </div>
      
      <input
        type="file"
        className="sr-only"
        ref={handleInputRef}
        onChange={handleChange}
        {...props}
      />
    </div>
  );
});

// Set display name for React DevTools
FileInput.displayName = "FileInput";

export { FileInput };