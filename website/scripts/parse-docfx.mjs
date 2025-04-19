import * as fs from 'fs';
import { join, relative, dirname } from  'path';
import { stringify } from 'querystring';

// Set input and output directories
const inputDir = '../docfx-files';
const outputDir = '../content/docs/Backend';

// Makes sure the directory exists
function ensureDirSync(dirPath) 
{
    fs.mkdirSync(dirPath, { recursive: true });
}

function extractTypeWithLinks(line) 
{
    // Set patterns for types with links
    const markdownPattern = /\[([^\]]+)\]\(([^)]+)\)/g;
    const htmlPattern = /<a\s+href=['"]([^'"]+)['"]>([^<]+)<\/a>/g;
    
    // Clean line
    line = line.replace(/\\/g, '');
    
    const links = [];
    const typeTexts = [];
    
    // Check if line does not match, return null
    if (!line.match(markdownPattern) && !line.match(htmlPattern)) return null;
    
    // Process Markdown pattern
    if (line.match(markdownPattern)) {
        // Reset pattern before using exec in a loop
        markdownPattern.lastIndex = 0;
        
        // Collect all links and type names from markdown
        let match;
        while ((match = markdownPattern.exec(line)) !== null) {
            typeTexts.push(match[1]);
            links.push(match[2]);
        }
    } 
    // Process HTML pattern
    else if (line.match(htmlPattern)) {
        // Reset pattern before using exec in a loop
        htmlPattern.lastIndex = 0;
        
        // Collect all links and type names from HTML
        let match;
        while ((match = htmlPattern.exec(line)) !== null) {
            typeTexts.push(match[2]);  // In HTML pattern, group 2 contains the text
            links.push(match[1]);      // In HTML pattern, group 1 contains the link
        }
    }
    
    // Parse the full type string (handle generic structure with < and >)
    let fullType = line;
        
    // Clean up escaped characters like \< and \> to get the proper type name
    fullType = fullType.replace(/\\\\/g, '\\')
                        .replace(/\\</g, '<')
                        .replace(/\\>/g, '>');
                        
    // Check if we have a generic type (either by escaped < or regular <)
    const isGenericType = line.includes("\\<") || line.includes("<") || 
    (typeTexts.length > 1 && fullType.includes(">"));
    
    if (isGenericType && typeTexts.length > 1) {
        // We have a generic type like Task<BLOB_STATUSCODE>
        return {
            type: `${typeTexts[0]}<${typeTexts.slice(1).join(', ')}>${line.includes('>[]') ? '[]' : ''}`, // Full type name
            links: links, // All links in order
            description: ''
        };
    } else {
        // Regular non-generic type
        return {
            name: '',
            type: typeTexts[0],
            links: links,
            description: ''
        };
    }
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
    // If a file has the same name as a folder, move it into the folder and rename it index.mdx
    // e.g., Knowledgebank.Data.ResourceManager.mdx -> Knowledgebank/Data/ResourceManager/index.mdx
    const parts = file.split('.').slice(0, -1);
    const className = parts.join('.');
    
    const end = folders.includes(parts.at(-1)) ? "/index.mdx" : ".mdx";
    const outputLocation = join(outputDir, parts.join('/').concat(end));
    ensureDirSync(dirname(outputLocation));
    fs.copyFileSync(join(inputDir, file), outputLocation);
    
    fileLocations[className] = outputLocation;
    //console.log(`✔ Copied: ${className}`);
});

// Clean up the content of the files and make them compatible with fumadocs
Object.entries(fileLocations).forEach(([className, file]) => 
{
    // Extract content
    let content = fs.readFileSync(file, 'utf-8');
    
    // Add frontmatter heading (required for fumadocs)
    const frontmatter = `---\ntitle: ${className.split('.').at(-1)}\n---\n`;
    // Add imports
    const imports = [
        `import { CollapsibleInherited } from "@/components/collapsible"`,
        `import { TypeTable } from "@/components/type_table"`,
        `import { CSharpType } from "@/components/csharp_type"`,
    ];
    
    // Remove all HTML anchors
    content = content.replace(/<a\b[^>]*>(.*?)<\/a>/g, '');
    
    // Remove the big heading (single #)
    content = content.replace(/^#(?![#]).*$/m, '');
    
    // Escape all < characters (required for fumadocs)
    content = content.replace(/</g, '\\<');
    
    // Update links to point to the new location
    content = content.replace(/\[(.*?)\]\((.*?)\)/g, (match, p1, p2) => {
        // Get the class name from the link
        const _className = p2.slice(0, -3).replace(/\\/g, '');
        
        // If the class name does not have a file location, return original
        if (!(_className in fileLocations)) return match;
        
        // Get the relative path to the file location
        let relativePath = relative(file, fileLocations[_className])
                .replace(/\\/g, '/')                                        // Convert backslashes to slashes
                .replace('../', '')                                         // Remove the leading ../
                .replace('index', '../' + _className.split('.').at(-1))     // Add the class name to the path if we go a level up
                .replace('.mdx', '');                                       // Remove the .mdx extension
        
        // If the file is an index file, add the class name to the path        
        if (file.includes('index')) relativePath = className.split('.').at(-1) + '/' + relativePath;
        
        // Return formatted link
        return `<a href='${relativePath}'>${p1}</a>`;
    });
    
    // Process the content to place it in nice components
    const lines = content.split('\n');
    const membersList = [];
    const linksList = [];
    const outputLines = [];
    const parametersList = [];
    const exceptionsList = [];

    let foundInherited = false;
    let foundParameters = false;
    let foundExceptions = false;
    let foundReturns = false;
    
    let foundParameterCount = 0;
    let foundExceptionCount = 0;
    
    lines.forEach(line => 
    {
        // Make the classes lists nice
        if (file.includes('index') && line.includes('</a>')) 
        {
            // If we have a link, push it to the list, do nothing with it yet
            linksList.push(line);
            return;
        }
        // Only do something if there is something in the list
        else if (linksList.length > 0)
        {
            // If line is empty, ignore
            if (line.trim() === "") return;
            
            // If we have exactly one link, just add it normally
            if (linksList.length == 1) 
            {
                outputLines.push(linksList[0]);
            }
            else 
            {
                // If we have multiple links, add them to a list
                outputLines.push(`<ul className='list-disc'>`);
                linksList.forEach(link => outputLines.push(`<li>${link.replace(/[\r\n]+/g, '')}</li>`));
                outputLines.push(`</ul>`);
            }
            
            // Clear the list for the next iteration
            linksList.length = 0;
            
            return;
        }
        
        
        
        // Check for the start of the Inherited Members section
        if (line.match(/^#{2,4}\s+Inherited Members/)) 
        {
            // Found the Inherited Members section
            foundInherited = true;
            outputLines.push(`\n`);
            return;
        }
        
        // If we found the Inherited Members section, add the members to the list
        if (foundInherited) 
        {
            // If the start was found, add to memberslist instead
            if (line.trim() !== "")
            {
                // Add if line is not empty
                membersList.push(line);
                return;
            }
            
            // If no members, skip empty line
            if (membersList.length === 0) return;
            
            // If line is empty, construct the collapsible
            outputLines.push(`<CollapsibleInherited title='Show Inherited Members (${membersList.length})'>`);
            membersList.forEach(member => 
            {
                // Add each member to the accordeon
                outputLines.push(`${member}`);
            });
            outputLines.push(`</CollapsibleInherited>\n`);
            foundInherited = false;
            
            return;
        }
        
        
        
        // Find the parameter section and make it into a table
        if (line.match(/^#{2,4}\s+Parameters/)) 
        {
            // Found the Parameters section
            foundParameters = true;
            foundParameterCount = 0;
            parametersList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Parameters</h4>`);
            return;
        }
        
        
        // If we found the Parameters section, add the members to the list
        if (foundParameters) 
        {
            foundParameterCount++;
        
            // Ignore empty lines
            if (line.trim() === "") return;
            
            // Set parameter pattern to match the parameter format
            const markdownPattern = /`([^`]+)`\s+\[([^\]]+)\]\(([^)]+)\)/g;
            const htmlPattern = /`([^`]+)`\s+<a\s+href=['"]([^'"]+)['"]>([^<]+)<\/a>/g;
            
            // Check if line contains a parameter
            if (line.match(markdownPattern) || line.match(htmlPattern)) 
            {
                const match = line.match(markdownPattern) ? markdownPattern.exec(line) : htmlPattern.exec(line);
                
                const typeWithLinks = extractTypeWithLinks(line);
                typeWithLinks.name = match[1];
                
                parametersList.push(typeWithLinks);
                
                foundParameterCount = 0;
                
                return;
            }
            // We did not match but was still parameterFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the parameter did not have a description
            else if (parametersList.length > 0)
            {
                // If we have a description line, add it to the last parameter
                if (foundParameterCount == 2 && !line.startsWith('#')) 
                {
                    parametersList[parametersList.length - 1].description = line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundParameterCount > 2 || line.startsWith('#'))
                {
                    outputLines.push(`<TypeTable types={${JSON.stringify(parametersList)}} />\n`);
                    foundParameters = false;
                }
            }
        }
        
        // Find the exception section and make it into a table
        if (line.match(/^#{2,4}\s+Exceptions/)) 
        {
            // Found the Exceptions section
            foundExceptions = true;
            foundExceptionCount = 0;
            exceptionsList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Exceptions</h4>`);
            return;
        }
        
        
        // If we found the Exceptions section, add the members to the list
        if (foundExceptions) 
        {
            foundExceptionCount++;
        
            // Ignore empty lines
            if (line.trim() === "") return;
            
            const typeWithLinks = extractTypeWithLinks(line);
            
            // Check if line contains a exception
            if (typeWithLinks) 
            {
                exceptionsList.push(typeWithLinks);
                
                foundExceptionCount = 0;
                
                return;
            }
            // We did not match but was still exceptionFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the exception did not have a description
            else if (exceptionsList.length > 0)
            {
                // If we have a description line, add it to the last exception
                if (foundExceptionCount == 2 && !line.startsWith('#')) 
                {
                    exceptionsList[exceptionsList.length - 1].description = line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundExceptionCount > 2 || line.startsWith('#'))
                {
                    outputLines.push(`<TypeTable types={${JSON.stringify(exceptionsList)}} />\n`);
                    foundExceptions = false;
                }
            }
        }
        
        // Check for the start of the Returns section
        if (line.match(/^#{2,4}\s+Returns/)) 
        {
            // Found the Returns section
            foundReturns = true;
            return;
        }
        
        // If we found the Returns section, display formatted return type
        if (foundReturns) 
        {
            // If line is empty, ignore
            if (line.trim() === "") return;
            
            const typeWithLinks = extractTypeWithLinks(line);
            
            outputLines.push(`<h4 className='mb-0'>Returns:</h4><CSharpType type='${typeWithLinks.type}' links={${JSON.stringify(typeWithLinks.links)}} />`);
        
            foundReturns = false;
            return;
        }
        
        // Normal line, add to output
        outputLines.push(line);
    });
    
    content = outputLines.join('\n');
    
    // Write adjusted content to the file
    fs.writeFileSync(file, frontmatter + imports.join('\n') + '\n' + content, 'utf-8');
    //console.log(`✔ Processed: ${className}`); 
});

console.log("Done!");