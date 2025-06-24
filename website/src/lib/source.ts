import { docs, guide } from "@/../.source";
import { loader } from "fumadocs-core/source";
import { createOpenAPI } from "fumadocs-openapi/server";

export const docsSource = loader({
    baseUrl: "/docs",
    source: docs.toFumadocsSource(),
});

export const guideSource = loader({
    baseUrl: "/guide",
    source: guide.toFumadocsSource(),
});

export const openapi = createOpenAPI({
    disablePlayground: true,
});


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


