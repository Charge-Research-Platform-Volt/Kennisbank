import type { NextConfig } from "next";
import { createMDX } from 'fumadocs-mdx/next';

const withMDX = createMDX();

const nextConfig: NextConfig = {
	output: "export", // Changed from "standalone" to "export" for GitHub Pages
	trailingSlash: true, // Required for GitHub Pages
	reactStrictMode: true, // Enables React's Strict Mode

	// GitHub Pages static export configuration
	images: {
		unoptimized: true, // Required for static export
	},

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