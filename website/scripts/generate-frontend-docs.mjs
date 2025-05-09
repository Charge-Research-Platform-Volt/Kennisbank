import fs from "fs/promises";
import path from "path";
import { exec } from "child_process";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const frontendDir = path.resolve(__dirname, "../../frontend");
const typedocOutDir = path.resolve(__dirname, "../typedoc-files");
const finalDocsDir = path.resolve(__dirname, "../content/docs/Frontend");

console.log("✅ Step 1: Generating TypeDoc...");
await fs.rm(typedocOutDir, { recursive: true, force: true });
await new Promise((resolve, reject) => {
  exec("npx typedoc", { cwd: frontendDir }, (err, stdout, stderr) => {
    if (err) {
      console.error("❌ TypeDoc error:", stderr);
      return reject(err);
    }
    console.log(stdout);
    resolve();
  });
});

// Recursive conversion from .md -> .mdx + linkfix
async function convertMarkdownToMdx(srcDir, destDir) {
  const entries = await fs.readdir(srcDir, { withFileTypes: true });

  for (const entry of entries) {
    const srcPath = path.join(srcDir, entry.name);
    const isMd = entry.isFile() && entry.name.endsWith(".md");

    if (entry.isDirectory()) {
      await convertMarkdownToMdx(srcPath, path.join(destDir, entry.name));
    } else if (isMd) {

      const rawName = entry.name === "README.md"
        ? (path.basename(srcDir) === "typedoc-files" ? "Frontend" : path.basename(srcDir))
        : entry.name.replace(".md", "");
      const title = rawName.charAt(0).toUpperCase() + rawName.slice(1);

      let content = await fs.readFile(srcPath, "utf-8");

      content = content
        .replace(/\]\(([^)]+)\.md\)/g, "]($1)") // Remove .md from links
        .replace(/\/README(?:\.md)?/g, "") // Remove /README.md from links
        .replace(/\/\(([^)]+)\)/g, '/$1'); // Fix links with parentheses
      
      const frontmatter = `---\ntitle: ${title}\nsidebar_position: 1\n---\n\n`;


      const destName = entry.name.replace(".md", ".mdx").replace("README.mdx", "index.mdx");

      // Fix links to point to the correct file if it is an index file (as an index file hasn't his name in the path already)
      if(destName === "index.mdx") {
        content = content.replace(/\(([^()\/]+\/)/g, `(./${rawName}/$1`) // Add title to links
      }

      const destPath = path.join(destDir, destName)
        .replace(/\/\(([^)]+)\)/g, '/$1')
        .replace(/\\\(([^)]+)\)/g, '\\$1');

      await fs.mkdir(path.dirname(destPath), { recursive: true });
      await fs.writeFile(destPath, frontmatter + content, "utf-8");
    }
  }
}

console.log("✅ Step 2: Convert all .md files to .mdx...");
await fs.rm(finalDocsDir, { recursive: true, force: true });
await convertMarkdownToMdx(typedocOutDir, finalDocsDir);

console.log("🎉 Done! Result files can be found in:", finalDocsDir);
