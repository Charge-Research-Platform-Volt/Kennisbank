// utils/useDrawerRerender.ts
import { useEffect, useState } from 'react';

/**
 * A minimal hook that forces a React re-render cycle when any drawer open state changes.
 * This helps ensure proper positioning of UI components like selection dropdowns within drawers.
 * 
 * @param openStates An array of boolean open states to monitor
 */
export function useDrawerRerender(openStates: boolean[]): void {
  // State used solely to trigger a re-render
  const [_, setRerender] = useState(0);
  
  useEffect(() => {
    // When any open state changes (especially to true),
    // schedule a state update to force an additional render cycle
    const isAnyOpen = openStates.some(state => state);
    
    if (isAnyOpen) {
      // Use setTimeout with 0 delay to push to next event loop tick
      // This ensures the DOM has had a chance to update first
      setTimeout(() => {
        setRerender(prev => prev + 1);
      }, 0);
    }
  }, [...openStates]);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


