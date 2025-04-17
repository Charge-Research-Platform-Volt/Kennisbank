import * as fs from 'fs';
import { join, relative, basename, dirname } from  'path';
import { arrayBuffer } from 'stream/consumers';

// Set input and output directories
const inputDir = '../docfx-files';
const outputDir = '../content/docs/backend';

// Makes sure the directory exists
function ensureDirSync(dirPath) 
{
    fs.mkdirSync(dirPath, { recursive: true });
}



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

// Place the files at the correct location
files.forEach(file => 
{   
    // Copy file to new location (using folder structure inferred from file name)
    // If a file has the same name as a folder, move it into the folder and rename it index.md
    // e.g., Knowledgebank.Data.ResourceManager.md -> Knowledgebank/Data/ResourceManager/index.md
    const parts = file.split('.').slice(0, -1);
    const className = parts[parts.length - 1];
    const end = folders.includes(className) ? "/index.md" : ".md";
    const outputLocation = join(outputDir, parts.join('/').concat(end));
    ensureDirSync(dirname(outputLocation));
    fs.copyFileSync(join(inputDir, file), outputLocation);
    
    let content = fs.readFileSync(outputLocation, 'utf-8');
    
    const frontmatter = `---\ntitle: ${className}\n---\n`;
    
    fs.writeFileSync(outputLocation, frontmatter + content, 'utf-8');
    console.log(`✔ Processed: ${className}`);
});

console.log("Done!");