import { pino, type Logger } from "pino";
import path from "path";

export const Log: Logger = pino({
    level: process.env.NODE_ENV === "production" ? "info" : "debug",

    transport:
        process.env.NODE_ENV === "production"
            ? {
                  target: "pino/file",
                  options: {
                      destination: path.join(process.cwd(), "src/logs/app.log"),
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
                              destination: path.join(
                                  process.cwd(),
                                  "src/logs/dev.log",
                              ),
                          },
                          level: "debug",
                      },
                  ],
              },
});
