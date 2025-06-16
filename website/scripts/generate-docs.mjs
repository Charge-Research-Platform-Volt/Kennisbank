import * as OpenAPI from "fumadocs-openapi";
import { rimrafSync } from "rimraf";
import fs from "node:fs/promises";
import path from "node:path";

const out = "./content/docs/(api)";
const swaggerURL = `http://localhost:8080/swagger/v1/swagger.json`;
const swaggerFilePath = "./swagger.json";
const publicSwaggerPath = "./public/swagger.json";

// Fix unsupported media types in swagger data
function fixMediaTypes(swaggerData) {
    const data = JSON.parse(JSON.stringify(swaggerData)); // Deep clone
    
    // Map of unsupported -> supported media types
    const mediaTypeMap = {
        'text/json': 'application/json',
        'application/json; charset=utf-8': 'application/json',
        'text/json; charset=utf-8': 'application/json',
        'application/*+json': 'application/json', // Add this new one
        'application/problem+json': 'application/json',
        'application/vnd.api+json': 'application/json',
    };
    
    function processObject(obj) {
        if (typeof obj !== 'object' || obj === null) return;
        
        for (const key in obj) {
            if (key === 'content' && typeof obj[key] === 'object') {
                const content = obj[key];
                const contentKeys = Object.keys(content);
                
                for (const mediaType of contentKeys) {
                    if (mediaTypeMap[mediaType]) {
                        // Replace unsupported media type with supported one
                        content[mediaTypeMap[mediaType]] = content[mediaType];
                        delete content[mediaType];
                        console.log(`Fixed media type: ${mediaType} -> ${mediaTypeMap[mediaType]}`);
                    }
                }
            } else if (typeof obj[key] === 'object') {
                processObject(obj[key]);
            }
        }
    }
    
    processObject(data);
    return data;
}

// Fix document paths in generated MDX files
async function fixDocumentPaths() {
    console.log("Fixing document paths in generated files...");
    
    async function processDirectory(dirPath) {
        const entries = await fs.readdir(dirPath, { withFileTypes: true });
        
        for (const entry of entries) {
            const fullPath = path.join(dirPath, entry.name);
            
            if (entry.isDirectory()) {
                await processDirectory(fullPath);
            } else if (entry.name.endsWith('.mdx')) {
                let content = await fs.readFile(fullPath, 'utf-8');
                
                // Replace the document path to point to public directory correctly
                const originalContent = content;
                content = content.replace(
                    /document=\{"\.\/swagger\.json"\}/g,
                    'document={"/swagger.json"}'
                );
                
                if (content !== originalContent) {
                    await fs.writeFile(fullPath, content);
                    console.log(`Fixed document path in: ${path.relative(process.cwd(), fullPath)}`);
                }
            }
        }
    }
    
    await processDirectory(out);
    console.log("Document paths fixed!");
}

// Fetch Swagger JSON from the API endpoint, with delay because the API may not be ready yet.
async function fetchSwaggerJSON() {
    const MAX_RETRIES = 20;
    const RETRY_DELAY = 2000;

    // Ensure public directory exists
    await fs.mkdir("./public", { recursive: true });

    for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
        try {
            console.log(
                `Fetching Swagger JSON from ${swaggerURL} (attempt ${attempt}/${MAX_RETRIES})...`,
            );
            const response = await fetch(swaggerURL);

            if (!response.ok) {
                throw new Error(
                    `Failed to fetch Swagger JSON: ${response.statusText}`,
                );
            }

            const swaggerData = await response.json();
            
            // Fix media types before saving
            const fixedSwaggerData = fixMediaTypes(swaggerData);
            const swaggerContent = JSON.stringify(fixedSwaggerData, null, 2);

            // Save to root for build process
            await fs.writeFile(swaggerFilePath, swaggerContent);
            console.log(`Swagger JSON saved to ${swaggerFilePath}`);
            
            // Save to public for runtime access
            await fs.writeFile(publicSwaggerPath, swaggerContent);
            console.log(`Swagger JSON saved to ${publicSwaggerPath}`);

            return true;
        } catch (error) {
            console.error(
                `Error fetching Swagger JSON (attempt ${attempt}/${MAX_RETRIES}): ${error.message}`,
            );
            if (attempt < MAX_RETRIES) {
                console.log(`Retrying in ${RETRY_DELAY / 1000} seconds...`);
                await new Promise((resolve) =>
                    setTimeout(resolve, RETRY_DELAY),
                );
            } else {
                console.error("Max retry attempts reached. Giving up.");
                return false;
            }
        }
    }

    return false;
}

// Clean generated files
rimrafSync(out, {
    filter(v) {
        return !v.endsWith("index.mdx") && !v.endsWith("meta.json");
    },
});

// Fetch swagger.json and generate documentation
async function main() {
    const success = await fetchSwaggerJSON();

    if (!success) {
        console.error(
            `Failed to fetch Swagger JSON. Make sure the API is running at ${process.env.API_URL}`,
        );
        process.exit(1);
    }

    console.log("Generating API documentation...");
    await OpenAPI.generateFiles({
        // input files
        input: [swaggerFilePath],
        output: out,
        per: "operation",
        groupBy: "tag",
    });

    // Fix the document paths after generation
    await fixDocumentPaths();

    console.log("API documentation generated successfully!");
}

// Run the main function
main().catch((error) => {
    console.error("Error generating documentation:", error);
    process.exit(1);
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)