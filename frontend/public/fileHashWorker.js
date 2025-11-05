// Import js-sha256 from CDN for true streaming hash support
importScripts('https://cdn.jsdelivr.net/npm/js-sha256@0.11.0/src/sha256.min.js');

self.onmessage = async function(e) {
    const { id, file } = e.data;

    try {
        // Process file in chunks for progress reporting and memory efficiency
        const CHUNK_SIZE = 64 * 1024 * 1024; // 64MB chunks
        const fileSize = file.size;
        let offset = 0;

        // Initialize streaming hash
        const hash = sha256.create();

        while (offset < fileSize) {
            const chunk = file.slice(offset, offset + CHUNK_SIZE);
            const arrayBuffer = await chunk.arrayBuffer();

            // Update hash incrementally (doesn't store the chunk in memory)
            hash.update(new Uint8Array(arrayBuffer));

            offset += CHUNK_SIZE;

            // Report progress
            const progress = Math.min((offset / fileSize) * 100, 100);
            self.postMessage({ id, progress });
        }

        // Finalize hash
        const hashHex = hash.hex();

        self.postMessage({ id, hash: hashHex, progress: 100 });
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        self.postMessage({ id, error: errorMessage });
    }
};
