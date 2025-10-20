'use client'

import { useState, useEffect, useCallback } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useBeforeUnload } from '@/hooks/useBeforeUnload';
import UnsavedDialog from '@/components/ui/unsaved-dialog';

/**
 * @summary Hook that checks for navigation changes. Opens the UnsavedDialog when there are unsaved changes.
 * @param hasUnsavedChanges Determines if there are any unsaved changes.
 * @returns An UnsavedDialog when there are unsaved changes.
 */
export function useNavigationWarning(hasUnsavedChanges: boolean) {
  const router = useRouter();
  const pathname = usePathname();
  const [pendingUrl, setPendingUrl] = useState<string | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  
  // Reuse your existing hook for browser tab/window close events
  useBeforeUnload(hasUnsavedChanges);

  // Set up event listener for Next.js navigation events
  useEffect(() => {
    if (!hasUnsavedChanges) return;

    // Intercept clicks on links
    const handleLinkClick = (e: MouseEvent) => {
      // Check if it's a link click
      const target = e.target as HTMLElement;
      const link = target.closest('a');
      
      if (!link) return;
      
      // Skip if it's an external link or has target="_blank"
      if (
        link.target === '_blank' || 
        link.getAttribute('rel') === 'external' ||
        link.getAttribute('href')?.startsWith('http')
      ) return;
      
      const href = link.getAttribute('href');
      if (!href || href === pathname || href === '#') return;

      // Prevent the default navigation
      e.preventDefault();
      e.stopPropagation();
      
      // Show confirmation dialog
      setPendingUrl(href);
      setShowDialog(true);
    };

    // Add the event listener
    document.addEventListener('click', handleLinkClick, true);
    
    return () => {
      document.removeEventListener('click', handleLinkClick, true);
    };
  }, [hasUnsavedChanges, pathname]);

  // Handle dialog confirmation - proceed with navigation
  const handleConfirmNavigation = useCallback(() => {
    setShowDialog(false);
    if (pendingUrl) {
      router.push(pendingUrl);
      setPendingUrl(null);
    }
  }, [pendingUrl, router]);

  // Handle dialog cancellation
  const handleCancelNavigation = useCallback(() => {
    setShowDialog(false);
    setPendingUrl(null);
  }, []);

  // The dialog component
  const navigationDialog = (
    <UnsavedDialog
      open={showDialog}
      onOpenChange={setShowDialog}
      onConfirmation={handleConfirmNavigation}
      onCancel={handleCancelNavigation}
    />
  );

  return navigationDialog;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


