import type { NextConfig } from "next";
import { config } from "dotenv";
import path from "path";
import fs from "fs";

// Load environment variables from parent directory's .env.local file (local development only)
const envPath = path.join(__dirname, "..", ".env.local");
if (fs.existsSync(envPath)) {
  config({ path: envPath });
}

const nextConfig: NextConfig = {
  // Excludes pino and pino-pretty from the server bundle
  serverExternalPackages: ["pino", "pino-pretty"],

  //Reduces the size of the output in production
  output: "standalone",

  // Removes console.log() calls in production
  compiler: {
    removeConsole: process.env.NODE_ENV === "production",
  },

  // Pass environment variables to the client and server
  env: {
    API_URL: process.env.API_URL,
    HELP_URL: process.env.HELP_URL,
    NEXT_PUBLIC_TIMEZONE: process.env.NEXT_PUBLIC_TIMEZONE,
  },
};

export default nextConfig;


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


