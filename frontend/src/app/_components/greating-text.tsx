"use client"; 

import { useState, useEffect } from "react";

interface GreetingProps {
  initialHour: number;
  firstName: string;
}

// displays a different greeting dependent on what time of day it is
export default function Greeting({ initialHour, firstName }: GreetingProps) {
  let greeting : string = "";
  const currentHour : number = initialHour;
  if (currentHour >= 12 && currentHour < 18) {
    greeting = "Good afternoon";
  } else if (currentHour >= 18) {
    greeting = "Good evening";
  } else if (currentHour < 4){
    greeting = "Good night";
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
    } else if (currentHour < 4){
      setGreeting("Good night");
    }
    else {
      setGreeting("Good morning");
    }
  }, []);

  return <h1 className="text-5xl font-bold">{`${greetingState} ${firstName}`}</h1>;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


