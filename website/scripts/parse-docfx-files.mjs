import { readFileSync, writeFileSync, mkdirSync, readdirSync, statSync, rmdirSync } from 'fs';
import { join, relative } from 'path';

const inputDir = '../docfx-files';
const outputDir = '../content/docs/backend';

function ensureDirSync(dirPath) {
    try { rmdirSync(dirPath, { force: true }); } catch (e) { /* Ignore error */ }
    mkdirSync(dirPath, { recursive: true });
}

function convertAnchorsToHeadingIDs(markdown) {
  let firstRemoved = false;

  return markdown.replace(/^(\s*#+)\s*<a id="([^"]+)"><\/a>\s*(.+)$/gm, (_, hashes, id, title) => {
    if (!firstRemoved) 
    {
        firstRemoved = true;
        return '';
    }
    
    return `${hashes} ${title.trim()}`;
  }).replace(/^#\s*<a id="([^"]+)"><\/a>/gm, ''); // Remove the large top heading completely
}

function stripRemainingHtmlAnchors(markdown) {
  return markdown.replace(/<a\s+id="[^"]*"><\/a>/gi, '')
                 .replace(/<a\s+id="[^"]*"\s*>/gi, '')
                 .replace(/<\/a>/gi, '');
}

function extractDescription(markdown) {
  return '';
  // Return first non-heading paragraph
  const match = markdown.match(/\n(?!#)([^#\n][^\n]+)\n/);
  if (match && match[1]) {
    return match[1].trim().replace(/"/g, '\\"');
  }
  return '';
}

function updateLinks(markdown, mapping) {
  return markdown.replace(/\]\(([^)]+\.md)\)/g, (match, linkPath) => {
    const cleanPath = linkPath.replace(/^\.?\//, '').replace(/\\/g, '/');
    const baseName = cleanPath.split('/').pop(); // e.g., Knowledgebank.Data.ResourceManager.md

    const updated = mapping[cleanPath] || mapping[baseName];
    if (updated) {
      const noExt = updated.replace(/\/?index\.md$/, '').replace(/\.md$/, '');
      return `](${noExt})`;
    }

    return match;
  });
}

function moveAndFormatFile(filePath, mapping) {
  if (!filePath.endsWith('.md')) return;

  const relativePath = relative(inputDir, filePath).replace(/\\/g, '/');
  const baseName = filePath.split(/[/\\]/).pop().replace(/\.md$/, ''); // Just the filename, no path
  const parts = baseName.split('.');
  const className = parts.pop();
  const folderPath = join(outputDir, ...parts);
  const newFilePath = join(folderPath, `${className}.md`);

  ensureDirSync(folderPath);

  let content = readFileSync(filePath, 'utf8');

  // Sanitize content
  content = convertAnchorsToHeadingIDs(content);
  content = stripRemainingHtmlAnchors(content);

  const description = extractDescription(content);
  content = updateLinks(content, mapping);

  const frontmatter = `---\ntitle: ${className}\ndescription: "${description}"\n---\n\n`;

  writeFileSync(newFilePath, frontmatter + content);
  console.log(`✔ Processed: ${newFilePath}`);

  // Map original to new relative path for links
  const mappingPath = relative(outputDir, newFilePath).replace(/\\/g, '/');
  mapping[relativePath] = mappingPath;
  mapping[className + '.md'] = mappingPath;
}

function walkDirAndProcess(dir, mapping) {
  readdirSync(dir).forEach(item => {
    const fullPath = join(dir, item);
    const stat = statSync(fullPath);
    if (stat.isDirectory()) {
      walkDirAndProcess(fullPath, mapping);
    } else {
      moveAndFormatFile(fullPath, mapping);
    }
  });
}

const linkMapping = {};
walkDirAndProcess(inputDir, linkMapping);
