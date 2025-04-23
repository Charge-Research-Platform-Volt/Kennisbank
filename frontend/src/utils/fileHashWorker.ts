import { ExistsResponseSchema } from "@/types/storage.type";

const BACKEND_API_URL = "http://localhost:8080/";
const BACKEND_API_EXIST_ROUTE = "storage/exists/";

/**
 * Calculates SHA-256 hash of a file on the client browser
 * using a web worker to prevent blocking the UI
 */
export function createFileHasher() {
  let worker: Worker | null = null;
  let workerObjectUrl: string | null = null;

  function getWorker(): Worker {
    if (!worker) {
      // Script to be executed on the webworker
      const workerScript = `self.onmessage = async function(e) {
                const { id, file } = e.data;

                try {
                    const arrayBuffer = await file.arrayBuffer();

                    const hashBuffer = await crypto.subtle.digest('SHA-256', arrayBuffer);

                    const hashArray = Array.from(new Uint8Array(hashBuffer));
                    const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');

                    self.postMessage({id, hash: hashHex});
                }catch (error) {
                    const errorMessage = error instanceof Error ? error.message : String(error);
                    self.postMessage({ id, error: errorMessage });
                }
            };` as string;

      // Create the webworker and get its url
      const blob = new Blob([workerScript], { type: "application/javascript" });
      const workerUrl = URL.createObjectURL(blob);
      worker = new Worker(workerUrl);
      workerObjectUrl = workerUrl;

      // Define what to do when the worker sends a message
      worker.onmessage = (event) => {
        const { id, hash, error } = event.data;

        // Check if the message sent is for something we are waiting on
        if (pendingRequests.has(id)) {
          const { resolve, reject } = pendingRequests.get(id);
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
   */
  function hashFile(file: File): Promise<string> {
    const worker = getWorker();

    // Send file to worker to hash
    return new Promise((resolve, reject) => {
      const id: number = nextId++;

      pendingRequests.set(id, { resolve, reject });

      worker.postMessage({ id, file });
    });
  }

  /**
   * Check if a file is a duplicate by hashing it and checking with the server
   */
  async function checkDuplicate(file: File): Promise<{ hash: string; isDuplicate: boolean; id: string }> {
    const hash = await hashFile(file);

    // Ask backend if the file already exists
    const urlSafeHash = encodeURIComponent(hash);
    const response = await fetch(`/api/storage/exists/${urlSafeHash}`, { credentials: "include" });

    // Parse response data and return
    if (response.ok) {
      const rawData = await response.json();

      try {
        const existsResponse = ExistsResponseSchema.parse(rawData);
        return {
          hash,
          isDuplicate: existsResponse.exists,
          id: existsResponse.id,
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

    if (workerObjectUrl) {
      URL.revokeObjectURL(workerObjectUrl);
      workerObjectUrl = null;
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
