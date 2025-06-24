import { exec } from 'child_process';
import { resolve, dirname, basename } from 'path';
import { rmSync } from 'fs';

// Path to your MJS script
const mjsScriptPath = resolve('./scripts/parse-docfx.mjs');
const mjsDirectory = dirname(mjsScriptPath);

// Target folder where docfx metadata should run
const targetFolder = resolve('../backend');

// First delete docfx-files folder
console.log("Removing old DocFX Files...");
rmSync('./docfx-files', { recursive: true, force: true });

// First run docfx metadata command in the target folder
console.log(`Running docfx metadata in ${targetFolder}`);

// Store original directory so we can return to it
const originalDir = process.cwd();

// Change directory to the target folder
process.chdir(targetFolder);

// Execute docfx metadata command
exec('docfx metadata', (error, stdout, stderr) => {
  if (error) {
    console.error(`Error executing docfx metadata: ${error.message}`);
    // Change back to original directory before exiting
    process.chdir(originalDir);
    return;
  }
  
  if (stderr) {
    console.error(`docfx metadata stderr: ${stderr}`);
  }
  
  console.log(`docfx metadata stdout: ${stdout}`);
  
  // Change directory to where the MJS script is located
  process.chdir(mjsDirectory);
  console.log(`Changed directory to run MJS script from: ${mjsDirectory}`);
  
  // Run the MJS script using child_process to avoid ESM URL issues
  const mjsFileName = basename(mjsScriptPath);
  console.log(`Running: node ${mjsFileName}`);
  
  exec(`node ${mjsFileName}`, (error, stdout, stderr) => {
    if (error) {
      console.error(`Error executing MJS script: ${error.message}`);
      // Change back to original directory before exiting
      process.chdir(originalDir);
      return;
    }
    
    if (stderr) {
      console.error(`MJS script stderr: ${stderr}`);
    }
    
    console.log(`MJS script stdout: ${stdout}`);
    console.log('All tasks completed successfully');
    
    // Change back to original directory when done
    process.chdir(originalDir);
  });
});