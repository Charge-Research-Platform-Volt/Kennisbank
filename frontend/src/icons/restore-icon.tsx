import * as React from "react";

export default function RestoreIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" {...props}>
            <circle cx="12" cy="12" r="0" fill="currentColor">
                <animate fill="freeze" attributeName="r" begin="0.8s" dur="0.2s" values="0;2"/>
            </circle>
            <g fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
                <path strokeDasharray="48" strokeDashoffset="48" d="M4.25 14c0.89 3.45 4.02 6 7.75 6c4.42 0 8 -3.58 8 -8c0 -4.42 -3.58 -8 -8 -8c-2.39 0 -4.53 1.05 -6 2.71l-2 2.29">
                    <animate fill="freeze" attributeName="stroke-dashoffset" dur="0.6s" values="48;0"/>
                </path>
                <path fill="currentColor" strokeWidth="1" d="M5.63 7.38l0 0l0 0l0 0z" opacity="0">
                    <animate fill="freeze" attributeName="d" begin="0.6s" dur="0.2s" values="M5.63 7.38l0 0l0 0l0 0z;M5.63 7.38L3.5 5.25L3.5 9.5L7.75 9.5z"/>
                    <set fill="freeze" attributeName="opacity" begin="0.6s" to="1"/>
                </path>
            </g>
        </svg>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


