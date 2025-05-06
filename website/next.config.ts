import type { NextConfig } from "next";
import { createMDX } from 'fumadocs-mdx/next';

const withMDX = createMDX();

const nextConfig: NextConfig = {
	output: "standalone", //Reduces the size of the output
	reactStrictMode: true, // Enables React's Strict Mode

	env: {
		API_URL: process.env.API_URL,
	},

	// Disable ESLint during production builds
	eslint: {
		ignoreDuringBuilds: process.env.NODE_ENV === "production", // Disable ESLint in production
	},
};

export default withMDX(nextConfig);

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


