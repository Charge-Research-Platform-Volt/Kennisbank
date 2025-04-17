import * as fs from 'fs';
import { join, relative, dirname } from  'path';

// Set input and output directories
const inputDir = '../docfx-files';
const outputDir = '../content/docs/Backend';

// Makes sure the directory exists
function ensureDirSync(dirPath) 
{
    fs.mkdirSync(dirPath, { recursive: true });
}



// -----------------
// MAIN LOGIC:
// -----------------



// Remove current output dir
try { fs.rmSync(outputDir, { recursive: true, force: true })} catch (e) { /* Ignore error */ }

const files = fs.readdirSync(inputDir);

// Determine which parts become folders so we can generate index files
let folders = [];
files.forEach(file => 
{
    file.split('.').slice(0, -2).forEach(item => 
    {
        if (!folders.includes(item)) folders.push(item);
    });
});

let fileLocations = {};

// Place the files at the correct location
files.forEach(file => 
{   
    // Skip non-markdown files
    if (!file.endsWith('.md')) return;

    // Copy file to new location (using folder structure inferred from file name)
    // If a file has the same name as a folder, move it into the folder and rename it index.md
    // e.g., Knowledgebank.Data.ResourceManager.md -> Knowledgebank/Data/ResourceManager/index.md
    const parts = file.split('.').slice(0, -1);
    const className = parts.join('.');
    
    const end = folders.includes(parts.at(-1)) ? "/index.md" : ".md";
    const outputLocation = join(outputDir, parts.join('/').concat(end));
    ensureDirSync(dirname(outputLocation));
    fs.copyFileSync(join(inputDir, file), outputLocation);
    
    fileLocations[className] = outputLocation;
    console.log(`✔ Copied: ${className}`);
});

// Clean up the content of the files and make them compatible with fumadocs
Object.entries(fileLocations).forEach(([className, file]) => 
{
    // Extract content
    let content = fs.readFileSync(file, 'utf-8');
    
    // Add frontmatter heading (required for fumadocs)
    const frontmatter = `---\ntitle: ${className.split('.').at(-1)}\n---\n`;
    
    // Remove all HTML anchors
    content = content.replace(/<a\b[^>]*>(.*?)<\/a>/g, '');
    
    // Remove the big heading (single #)
    content = content.replace(/^#(?![#]).*$/m, '');
    
    // Update links to point to the new location
    content = content.replace(/\[(.*?)\]\((.*?)\)/g, (match, p1, p2) => {
            // Get the class name from the link
            const _className = p2.slice(0, -3);
            
            // If the class name does not have a file location, return original
            if (!(_className in fileLocations)) return match;
            
            // Get the relative path to the file location
            let relativePath = relative(file, fileLocations[_className])
                    .replace(/\\/g, '/')                                        // Convert backslashes to slashes
                    .replace('../', '')                                         // Remove the leading ../
                    .replace('index', '../' + _className.split('.').at(-1))     // Add the class name to the path if we go a level up
                    .replace('.md', '');                                        // Remove the .md extension
            
            // If the f ile is the index of a folder, include the folder name in the path
            if (file.includes("index.md")) relativePath = className.split('.').at(-1) + "/" + relativePath;
            
            // Return formatted link
            return `[${p1}](${relativePath})`;
        });
    
    // Write adjusted content to the file
    fs.writeFileSync(file, frontmatter + content, 'utf-8');
    console.log(`✔ Processed: ${className}`); 
});

console.log("Done!");