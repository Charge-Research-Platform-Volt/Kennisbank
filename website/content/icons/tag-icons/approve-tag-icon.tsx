import * as React from "react";

export default function ApproveTagIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
      {...props} 
      width="30" 
      height="30" 
      viewBox="0 0 32 32" 
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        d="M26 24a1 1 0 0 0-1 1v4H7V3h10v7a1 1 0 0 0 1 1h7v4a1 1 0 1 0 2 0v-4.903c.003-.033.02-.063.02-.097a1 1 0 0 0-.421-.816l-7.892-7.891a1 1 0 0 0-.292-.195c-.031-.015-.063-.023-.097-.034a1 1 0 0 0-.253-.05C18.043 1.012 18.022 1 18 1H6a1 1 0 0 0-1 1v28a1 1 0 0 0 1 1h20a1 1 0 0 0 1-1v-5a1 1 0 0 0-1-1M19 9V4.414L23.586 9z"
        fill={fill}
      />
      <path
        fill={fill}
        d="M30.73 15.317a1 1 0 0 0-1.414-.047l-10.004 9.36-4.629-4.332a1 1 0 1 0-1.367 1.46l5.313 4.971a.997.997 0 0 0 1.368 0l10.688-10a1 1 0 0 0 .045-1.412"
      />
    </svg>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


