import { docs, guide } from "../../.source";
import { loader } from "fumadocs-core/source";
import { createOpenAPI } from "fumadocs-openapi/server";
import { attachFile } from "fumadocs-openapi/server";

export const docsSource = loader({
    baseUrl: "/docs",
    source: docs.toFumadocsSource(),
    pageTree: {
        attachFile,
    },
});

export const guideSource = loader({
    baseUrl: "/guide",
    source: guide.toFumadocsSource(),
});

export const openapi = createOpenAPI({
    disablePlayground: true,
});
