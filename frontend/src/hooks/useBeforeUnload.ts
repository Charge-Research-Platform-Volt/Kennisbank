'use client'

import { useEffect } from 'react';

/**
 * Hook to show a browser confirmation dialog when the user tries to close the tab
 * or navigate away from the page while unsaved changes exist.
 * 
 * @param hasUnsavedChanges Boolean indicating if there are unsaved changes
 * @param message Optional custom message (note: modern browsers typically show their own generic message)
 */
export function useBeforeUnload(hasUnsavedChanges: boolean, message: string = "You have unsaved changes. Are you sure you want to leave?") {
  useEffect(() => {
    // Only attach the event listener if there are unsaved changes
    if (!hasUnsavedChanges) return;

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      return message; // This triggers the confirmation dialog
    };
    
    window.addEventListener('beforeunload', handleBeforeUnload);
    
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [hasUnsavedChanges, message]);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


