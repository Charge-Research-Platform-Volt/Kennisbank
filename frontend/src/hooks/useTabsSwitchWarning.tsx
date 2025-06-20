'use client'

import React, { useState, useCallback, useRef, useEffect } from 'react';
import UnsavedDialog from '@/components/ui/unsaved-dialog';

interface UseTabSwitchWarningProps {
  /**
   * Whether there are unsaved changes that should trigger the warning
   */
  hasNonDefaultValues: boolean;
  
  /**
   * The currently active tab
   */
  activeTab: string;
  
  /**
   * Function to call when the tab should be changed
   */
  onTabChange: (tab: string) => void;
}

/**
 * @summary A hook that provides tab switching functionality with unsaved changes protection.
 * @returns An object with a requestTabChange function and a dialog element.
 */
export function useTabSwitchWarning({
  hasNonDefaultValues,
  activeTab,
  onTabChange
}: UseTabSwitchWarningProps) {
  // Store state for the dialog visibility
  const [showDialog, setShowDialog] = useState(false);
  
  // Store the pending tab that user wants to switch to
  const [pendingTab, setPendingTab] = useState<string | null>(null);
  
  // Use a ref to avoid closure issues with the callback
  const stateRef = useRef({
    hasNonDefaultValues,
    activeTab
  });
  
  // Keep the ref updated with the latest values
  useEffect(() => {
    stateRef.current = {
      hasNonDefaultValues,
      activeTab
    };
  }, [hasNonDefaultValues, activeTab]);
  
  // Function to request a tab change
  const requestTabChange = useCallback((newTab: string) => {
    // Don't do anything if clicking the current tab
    if (newTab === stateRef.current.activeTab) return;
    
    // Use the latest values from the ref to avoid stale closures
    if (stateRef.current.hasNonDefaultValues) {
      // Show confirmation dialog
      setPendingTab(newTab);
      setShowDialog(true);
    } else {
      // No unsaved changes, change tab immediately
      onTabChange(newTab);
    }
  }, [onTabChange]);
  
  // Function to handle confirmed tab change
  const handleConfirmedTabChange = useCallback(() => {
    // Hide the dialog
    setShowDialog(false);
    
    // If we have a pending tab, switch to it
    if (pendingTab) {
      onTabChange(pendingTab);
      // Reset the pending tab
      setPendingTab(null);
    }
  }, [pendingTab, onTabChange]);
  
  // Function to handle cancelled tab change
  const handleCancelTabChange = useCallback(() => {
    setShowDialog(false);
    setPendingTab(null);
  }, []);
  
  // Render the dialog only if it's meant to be shown
  const dialog = showDialog ? (
    <UnsavedDialog 
      open={true}
      onOpenChange={setShowDialog}
      onConfirmation={handleConfirmedTabChange}
      onCancel={handleCancelTabChange}
    />
  ) : null;
  
  return {
    requestTabChange,
    dialog
  };
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


