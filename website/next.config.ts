import type { NextConfig } from "next";
import { createMDX } from 'fumadocs-mdx/next';

const withMDX = createMDX();

const nextConfig: NextConfig = {
	output: "standalone", //Reduces the size of the output
};

export default withMDX(nextConfig);