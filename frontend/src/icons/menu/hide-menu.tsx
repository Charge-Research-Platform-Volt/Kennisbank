import * as React from "react";
import { SVGProps } from "react";

export default function HideMenu({ flip = false, flipArrow = false, ...props}: SVGProps<SVGSVGElement> & { flip?: boolean, flipArrow?: boolean }) {
  return (
    <svg {...props} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="15.059 15.75 14.94 13.59" transform={flip ? "scale(-1, 1)" : undefined}>
      <path
        strokeLinejoin="round"
        strokeLinecap="round"
        stroke="#8A8A8A"
        d="M20.834 16.833v11.334m-3-9h1m-1 2.666h1m-3 .667c0-2.5 0-3.75.636-4.626a3.33 3.33 0 0 1 .737-.737c.877-.637 2.127-.637 4.627-.637h1.333c2.5 0 3.75 0 4.626.637.283.205.532.454.737.737.637.876.637 2.127.637 4.626 0 2.5 0 3.75-.637 4.626a3.332 3.332 0 0 1-.737.737c-.876.637-2.127.637-4.626.637h-1.334c-2.5 0-3.75 0-4.625-.637a3.331 3.331 0 0 1-.738-.737c-.636-.876-.636-2.127-.636-4.626Z"
      />
      <path strokeLinejoin="round" strokeLinecap="round" stroke="#8A8A8A" d="m25.833 21.167-.817.705c-.344.296-.516.444-.516.628s.172.332.516.629l.817.705" transform={flipArrow ? "scale(-1, 1) translate(-50.5, 0)" : undefined} />
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


