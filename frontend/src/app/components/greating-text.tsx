"use client"; 

import { useState, useEffect } from "react";

export default function Greeting() {
  let greeting = "";
  const currentHour = new Date().getHours();
  if (currentHour >= 12 && currentHour < 18) {
    greeting = "Good afternoon";
  } else if (currentHour >= 18) {
    greeting = "Good evening";
  }
  else {
    greeting = "Good morning";
  }
  
  const [greetingState, setGreeting] = useState(greeting);

  useEffect(() => {
    const currentHour = new Date().getHours();
    if (currentHour >= 12 && currentHour < 18) {
      setGreeting("Good afternoon");
    } else if (currentHour >= 18) {
      setGreeting("Good evening");
    }
    else {
      setGreeting("Good morning");
    }
  }, []);

  return <h1 className="text-5xl font-bold">{greetingState}</h1>;
}