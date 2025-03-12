"use client"; 

import { useState, useEffect } from "react";

export default function Greeting() {
  const [greeting, setGreeting] = useState("Good morning");

  useEffect(() => {
    const currentHour = new Date().getHours();

    if (currentHour >= 12 && currentHour < 18) {
      setGreeting("Good afternoon");
    } else if (currentHour >= 18) {
      setGreeting("Good evening");
    }
  }, []);

  return <h1 className="text-5xl font-bold">{greeting}</h1>;
}