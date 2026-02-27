type Greeting = {
    heading: string;
    subtext: string;
};

type GreetingTemplate = {
    heading: (name: string) => string;
    subtext: string;
};

const morning: GreetingTemplate[] = [
    { heading: n => `Good morning, ${n}!`, subtext: "Ready to tackle the day?" },
    { heading: n => `Rise and shine, ${n}!`, subtext: "Let's get things done." },
    { heading: n => `Morning, ${n}!`, subtext: "Coffee's brewing, time to get started." },
    { heading: n => `Good morning, ${n}!`, subtext: "A fresh day, a fresh start." },
    { heading: n => `Hey ${n}, good morning!`, subtext: "What are we diving into today?" },
    { heading: n => `Morning, ${n}!`, subtext: "The early bird catches the worm." },
    { heading: n => `Good morning, ${n}!`, subtext: "Let's make today count." },
    { heading: n => `Hey, good morning ${n}!`, subtext: "Hope you slept well." },
    { heading: n => `Good morning, ${n}!`, subtext: "A new day, new possibilities." },
    { heading: n => `Morning, ${n}!`, subtext: "Ready to get to work?" },
    { heading: n => `Wakey wakey, ${n}!`, subtext: "Time to do something productive." },
    { heading: n => `Good morning, ${n}!`, subtext: "The day is yours." },
    { heading: n => `Morning, ${n}!`, subtext: "Let's see what today has in store." },
    { heading: n => `Good morning, ${n}!`, subtext: "Time to make some progress." },
    { heading: n => `Hey ${n}! Morning!`, subtext: "Where do you want to start?" },
];

const afternoon: GreetingTemplate[] = [
    { heading: n => `Good afternoon, ${n}!`, subtext: "How's the day treating you?" },
    { heading: n => `Hey ${n}!`, subtext: "Hope the morning was productive." },
    { heading: n => `Good afternoon, ${n}!`, subtext: "Keep that momentum going." },
    { heading: n => `Afternoon, ${n}!`, subtext: "Still plenty of day left." },
    { heading: n => `Hey ${n}, afternoon!`, subtext: "What are we working on?" },
    { heading: n => `Good afternoon, ${n}!`, subtext: "The grind continues." },
    { heading: n => `Afternoon, ${n}!`, subtext: "Let's make the most of it." },
    { heading: n => `Good afternoon, ${n}!`, subtext: "Ready for the second half?" },
    { heading: n => `Hey ${n}!`, subtext: "Afternoon already — time flies." },
    { heading: n => `Afternoon, ${n}!`, subtext: "Let's get back to it." },
    { heading: n => `Good afternoon, ${n}!`, subtext: "Coffee number two, perhaps?" },
    { heading: n => `Hey ${n}, good afternoon!`, subtext: "What's on the agenda?" },
    { heading: n => `Afternoon, ${n}!`, subtext: "Halfway through and still going strong." },
    { heading: n => `Good afternoon, ${n}!`, subtext: "Time to get things done." },
    { heading: n => `Hey ${n}!`, subtext: "The afternoon awaits." },
];

const evening: GreetingTemplate[] = [
    { heading: n => `Good evening, ${n}!`, subtext: "Wrapping up or just getting started?" },
    { heading: n => `Evening, ${n}!`, subtext: "The quiet hours are upon us." },
    { heading: n => `Hey ${n}, good evening!`, subtext: "Still at it?" },
    { heading: n => `Good evening, ${n}!`, subtext: "The day's winding down — let's make it count." },
    { heading: n => `Evening, ${n}!`, subtext: "Nothing like a productive evening." },
    { heading: n => `Good evening, ${n}!`, subtext: "Hope the day was kind to you." },
    { heading: n => `Hey ${n}!`, subtext: "Evening mode activated." },
    { heading: n => `Evening, ${n}!`, subtext: "The best ideas come after hours." },
    { heading: n => `Good evening, ${n}!`, subtext: "What are we finishing up tonight?" },
    { heading: n => `Hey ${n}, evening!`, subtext: "Squeezing in a bit more work?" },
    { heading: n => `Evening, ${n}!`, subtext: "Almost there for today." },
    { heading: n => `Good evening, ${n}!`, subtext: "A solid evening of work ahead." },
    { heading: n => `Hey ${n}!`, subtext: "Evening already — time flies." },
    { heading: n => `Evening, ${n}!`, subtext: "Let's close out the day strong." },
    { heading: n => `Good evening, ${n}!`, subtext: "Time to focus up." },
];

const lateNight: GreetingTemplate[] = [
    { heading: n => `Still up, ${n}?`, subtext: "Burning the midnight oil, I see." },
    { heading: n => `Hey ${n}!`, subtext: "The night shift, huh?" },
    { heading: n => `Up late, ${n}?`, subtext: "The night owls are in their element." },
    { heading: n => `Late night, ${n}!`, subtext: "You and the stars are both working overtime." },
    { heading: n => `Hey ${n}, up late?`, subtext: "The quiet of night — good for deep work." },
    { heading: n => `Burning the midnight oil, ${n}?`, subtext: "The dedicated ones always are." },
    { heading: n => `Late night grind, ${n}!`, subtext: "The office is yours at this hour." },
    { heading: n => `Hey ${n}!`, subtext: "Still going? Respect." },
    { heading: n => `Up late, ${n}!`, subtext: "Best time to get stuff done without interruptions." },
    { heading: n => `Night owl mode, ${n}!`, subtext: "The world is quiet — make it count." },
    { heading: n => `Hey ${n}, late night?`, subtext: "Don't forget to take a break." },
    { heading: n => `Still at it, ${n}?`, subtext: "The night shift crew never quits." },
    { heading: n => `Late night, ${n}!`, subtext: "Sometimes the best work happens after midnight." },
    { heading: n => `Hey ${n}!`, subtext: "The night is young... or is it?" },
    { heading: n => `Up late, ${n}?`, subtext: "The dedicated never rest." },
];

function pickRandom<T>(arr: T[]): T {
    return arr[Math.floor(Math.random() * arr.length)];
}

export function getGreeting(firstName: string): Greeting {
    const hour = new Date().getHours();

    let template: GreetingTemplate;
    if (hour < 12) {
        template = pickRandom(morning);
    } else if (hour < 18) {
        template = pickRandom(afternoon);
    } else if (hour < 21) {
        template = pickRandom(evening);
    } else {
        template = pickRandom(lateNight);
    }

    return {
        heading: template.heading(firstName),
        subtext: template.subtext,
    };
}
