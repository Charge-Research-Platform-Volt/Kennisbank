import { pino, type Logger } from "pino";
import path from "path";
import fs from "fs";

// Function to ensure directory exists
const ensureDirectoryExists = (filePath: string) => {
    const dirname = path.dirname(filePath);
    if (!fs.existsSync(dirname)) {
        fs.mkdirSync(dirname, { recursive: true });
    }
};

// Define log file paths
const productionLogPath = path.join(process.cwd(), "src/logs/app.log");
const devLogPath = path.join(process.cwd(), "src/logs/dev.log");

// Ensure log directories exist
if (process.env.NODE_ENV === "production") {
    ensureDirectoryExists(productionLogPath);
} else {
    ensureDirectoryExists(devLogPath);
}

export const Log: Logger = pino({
    level: process.env.NODE_ENV === "production" ? "info" : "debug",

    transport:
        process.env.NODE_ENV === "production"
            ? {
                  target: "pino/file",
                  options: {
                      destination: productionLogPath,
                  },
              }
            : {
                  targets: [
                      {
                          target: "pino-pretty",
                          options: {
                              colorize: true,
                          },
                          level: "debug",
                      },
                      {
                          target: "pino/file",
                          options: {
                              destination: devLogPath,
                          },
                          level: "debug",
                      },
                  ],
              },
});
