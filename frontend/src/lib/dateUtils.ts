/**
 * Helper function to convert dates to UTC ISO strings
 * For date-only values (YYYY-MM-DD), this creates a UTC date at midnight
 * to avoid timezone shifts that would change the date
 */
export const convertToUTCDate = (dateValue: Date | string): string => {
  // If it's already a Date object
  if (dateValue instanceof Date) {
    const utcDate = new Date(Date.UTC(
      dateValue.getFullYear(),
      dateValue.getMonth(),
      dateValue.getDate(),
      0, 0, 0, 0
    ));
    return utcDate.toISOString();
  }

  // If it's a string in YYYY-MM-DD format
  if (typeof dateValue === 'string' && /^\d{4}-\d{2}-\d{2}/.test(dateValue)) {
    const date = new Date(dateValue);
    const utcDate = new Date(Date.UTC(
      date.getFullYear(),
      date.getMonth(),
      date.getDate(),
      0, 0, 0, 0
    ));
    return utcDate.toISOString();
  }

  // For other formats, return as-is
  return dateValue.toString();
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
