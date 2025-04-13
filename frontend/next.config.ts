import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Excludes pino and pino-pretty from the server bundle
  serverExternalPackages: ["pino", "pino-pretty"],

  //Reduces the size of the output in production
  output: "standalone",

  // Removes console.log() calls in production
  compiler: {
    removeConsole: process.env.NODE_ENV === "production",
  },

  // Increases the maximum body size limit for server actions
  experimental: {
    serverActions: {
      bodySizeLimit: "100mb", 
    },
  },

  // Disable ESLint during production builds
  eslint: {
    ignoreDuringBuilds: process.env.NODE_ENV === "production", // Disable ESLint in production
  },
};

export default nextConfig;
