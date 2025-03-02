import type { NextConfig } from "next";

const nextConfig: NextConfig = {
	output: "standalone", //Reduces the size of the output

	compiler: {
		removeConsole: process.env.NODE_ENV === "production", //Removes console.log() calls in production
	},
};

export default nextConfig;
