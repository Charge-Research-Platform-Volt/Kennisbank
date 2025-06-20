import { useEffect, useState } from "react";
import { UseFormReturn } from "react-hook-form";

/**
 * Configuration for the useFormHasValues hook
 */
export interface FormValuesConfig {
  // Fields to ignore when checking for values
  ignoredFields?: string[];
  
  // Placeholder values to compare against (field name -> placeholder value)
  placeholders?: Record<string, any>;
  
  // Optional custom field validators
  customValidators?: Record<string, (value: any) => boolean>;
}

/**
 * Custom hook that checks if any field in the form has been filled out
 * @param form The form object from useForm
 * @param config Configuration options
 * @returns boolean indicating if any field has been modified
 */
export function useFormHasValues(
  form: UseFormReturn<any>,
  config: FormValuesConfig = {}
): boolean {
  const [hasValues, setHasValues] = useState<boolean>(false);
  
  // Watch all form values
  const values = form.watch();
  
  useEffect(() => {
    const { 
      ignoredFields = [], 
      placeholders = {}, 
      customValidators = {} 
    } = config;
    
    // Get form default values to use as base comparison
    const defaultValues = form.formState.defaultValues || {};
    
    // Filter out ignored fields
    const fieldsToCheck = Object.entries(values).filter(
      ([key]) => !ignoredFields.includes(key)
    );
    
    // Check if any field has a value
    const formHasValue = fieldsToCheck.some(([key, value]) => {
      // Skip fields that match their default or placeholder values
      const defaultValue = key in placeholders ? placeholders[key] : defaultValues[key];
      
      // Use custom validator if provided
      if (customValidators[key]) {
        return customValidators[key](value);
      }
      
      // Handle arrays
      if (Array.isArray(value)) {
        if (Array.isArray(defaultValue)) {
          // If default is empty array and current value is empty, no change
          if (value.length === 0 && defaultValue.length === 0) return false;
          
          // If lengths differ, it has changed
          if (value.length !== defaultValue.length) return true;
          
          // For simple arrays of primitives, stringify comparison works
          if (typeof value[0] !== 'object') {
            return JSON.stringify(value) !== JSON.stringify(defaultValue);
          }
          
          // For arrays of objects, we'd need deeper comparison
          // This is simplified for basic cases
          return value.some((item, index) => 
            JSON.stringify(item) !== JSON.stringify(defaultValue[index])
          );
        }
        // If default isn't an array but value is
        return value.length > 0;
      }
      
      // Handle File objects specially
      if (value instanceof File) {
        if (defaultValue instanceof File) {
          // Compare name and size
          return value.name !== defaultValue.name || 
                 (value.size > 0 && value.size !== defaultValue.size && value !== undefined);
        }
        return value.size > 0;
      }
      
      // Handle strings
      if (typeof value === "string") {
        if (typeof defaultValue === "string") {
          return value.trim() !== defaultValue.trim() && value.trim() !== "" && value !== undefined;
        }
        return value.trim() !== "" && value !== undefined;
      }
      
      // For objects (excluding Files)
      if (typeof value === 'object' && value !== null) {
        if (typeof defaultValue === 'object' && defaultValue !== null) {
          return JSON.stringify(value) !== JSON.stringify(defaultValue);
        }
        return Object.keys(value).length > 0;
      }
      
      // For other primitive types
      return value !== defaultValue && value !== undefined && value !== null;
    });
    
    setHasValues(formHasValue);
  }, [values, config, form.formState.defaultValues]);
  
  return hasValues;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


