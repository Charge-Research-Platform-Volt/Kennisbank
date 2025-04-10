"use client"; 

import { useState, useEffect } from "react";

// displays a different greeting dependent on what time of day it is
export default function Greeting() { 
  const [greetingState, setGreeting] = useState("Good");

  useEffect(() => {
    const currentHour = new Date().getHours();
    if (currentHour >= 12 && currentHour < 18) {
      setGreeting("Good afternoon");
    } else if (currentHour >= 18) {
      setGreeting("Good evening");
    } else if (currentHour < 4){
      setGreeting("Good night");
    }
    else {
      setGreeting("Good morning");
    }
  }, []);

  return <h1 className="text-5xl font-bold">{greetingState}</h1>;
}