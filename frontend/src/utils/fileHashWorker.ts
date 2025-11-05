import { ApiResponseSchema } from "@/types/apiResponse.type";

const BACKEND_API_URL = "/api/";
const BACKEND_API_EXIST_ROUTE = "resources/exists?hash=";

/**
 * Calculates SHA-256 hash of a file on the client browser
 * using a web worker to prevent blocking the UI
 */
export function createFileHasher() {
  let worker: Worker | null = null;

  function getWorker(): Worker {
    if (!worker) {
      // Use external worker file for proper library imports and streaming hash
      worker = new Worker("/fileHashWorker.js");

      // Define what to do when the worker sends a message
      worker.onmessage = (event) => {
        const { id, hash, error, progress } = event.data;

        // Check if the message sent is for something we are waiting on
        if (pendingRequests.has(id)) {
          const { resolve, reject, onProgress } = pendingRequests.get(id);

          // Handle progress updates
          if (progress !== undefined && hash === undefined && !error) {
            if (onProgress) {
              onProgress(progress);
            }
            return; // Don't delete the request yet, more updates coming
          }

          // Handle completion or error
          pendingRequests.delete(id);

          if (error) {
            reject(new Error(error));
          } else {
            resolve(hash);
          }
        }
      };
    }

    return worker;
  }

  // Store pending requests and ID counter
  const pendingRequests = new Map();
  let nextId: number = 1;

  /**
   * Calculate file hash in a worker thread
   * @param file - The file to hash
   * @param onProgress - Optional callback to receive progress updates (0-100)
   */
  function hashFile(file: File, onProgress?: (progress: number) => void): Promise<string> {
    const worker = getWorker();

    // Send file to worker to hash
    return new Promise((resolve, reject) => {
      const id: number = nextId++;

      pendingRequests.set(id, { resolve, reject, onProgress });

      worker.postMessage({ id, file });
    });
  }

  /**
   * Check if a file is a duplicate by hashing it and checking with the server
   * @param file - The file to check
   * @param onProgress - Optional callback to receive progress updates (0-100)
   */
  async function checkDuplicate(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<{ hash: string; isDuplicate: boolean; id: string }> {
    const hash = await hashFile(file, onProgress);

    // Ask backend if the file already exists
    const urlSafeHash = encodeURIComponent(hash);
    const response = await fetch(BACKEND_API_URL + BACKEND_API_EXIST_ROUTE + urlSafeHash, { credentials: "include" })

    // Parse response data and return
    if (response.ok) {
      const rawData = await response.json();

      try {
        const existsResponse = ApiResponseSchema.parse(rawData);
        return {
          hash,
          isDuplicate: existsResponse.body.exists,
          id: existsResponse.body.id,
        };
      } catch (error: unknown) {
        if (error instanceof Error) {
          throw new Error(`Invalid response format: ${error.message}`);
        }

        throw new Error(`Invalid response format: ${String(error)}`);
      }
    }

    throw new Error(`Server error: ${response.status}`);
  }

  /**
   * Dispose to free resources
   */
  function dispose() {
    if (worker) {
      worker.terminate();
      worker = null;
    }

    pendingRequests.clear();
  }

  return {
    hashFile,
    checkDuplicate,
    dispose,
  };
}

// Singleton for project wide use.
let fileHasher: ReturnType<typeof createFileHasher> | null = null;

/**
 * Get the file hasher instance (creates one if it does not exist)
 */
export function getFileHasher() {
  if (typeof window === "undefined") {
    // Don't allow running on the server
    return {
      hashFile: () => Promise.reject(new Error("This is a client only function.")),
      checkDuplicate: () => Promise.reject(new Error("This is a client only function.")),
      dispose: () => {},
    };
  }

  if (!fileHasher) {
    fileHasher = createFileHasher();
  }

  return fileHasher;
}

/**
 * Type for the file hasher instance
 */
export type FileHasher = ReturnType<typeof createFileHasher>;


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


