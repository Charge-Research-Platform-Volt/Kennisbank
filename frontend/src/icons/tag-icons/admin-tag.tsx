import * as React from "react";

export default function AdminIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
      {...props} 
      width="30" 
      height="30" 
      viewBox="0 0 30 30" 
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        d="M11.916 6.28125A4 4 0 007.916 10.28125a4 4 0 004 4 4 4 0 004-4 4 4 0 00-4-4zm-3.453 8.834c-3.238 1.374-5.378 4.508-5.48 8.024 2.19 2.503 5.4 4.091 9 4.091 1.966 0 3.81-.492 5.443-1.334-.443-.593-.816-1.242-1.13-1.94-1.99-2.189-2.497-4.823-2.558-7.386l-.006-.252-2.72 4.256-3.539-5.46z"
        fill={fill}
      />
      <path
        d="M22.037 15.309c-1.745 0-4.86 1.943-4.86 1.943.11 4.632 1.949 9.757 4.86 9.721h.01c.817-.01 1.54-.437 2.132-1.056.59-.62 1.076-1.444 1.474-2.37.797-1.85 1.242-4.108 1.242-5.97v-.267l-.225-.143s-.72-.458-1.642-.916c-.922-.458-2.02-.941-2.989-.941h-.004zm0 .97h.004c.6 0 1.685.409 2.556.842.729.362 1.123.614 1.299.723-.04 1.662-.438 3.696-1.137 5.318-.367.853-.81 1.587-1.284 2.085-.473.496-.95.746-1.439.754V16.28z"
        fill={fill}
      />
    </svg>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


