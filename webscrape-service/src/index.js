import express from 'express';
import { chromium } from 'playwright';
import * as cheerio from 'cheerio';
import { parseHTML } from 'linkedom';
import { Defuddle } from 'defuddle/node';

const PORT = process.env.PORT || 3050;
const NAVIGATION_TIMEOUT_MS = 15_000;
const REQUEST_TIMEOUT_MS = 30_000;

const emptyResult = { title: null, textContent: null, byline: null, excerpt: null, siteName: null };

// Lazily launch browser once, and reuse across all requests
let browserPromise = null;
function getBrowser() {
    browserPromise ??= chromium.launch({
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });
    return browserPromise;
}

// When Cheerio doesnt work (Defuddle can't find a clean main context block), 
// do it manually. Although this is a bit noisier.
function fallbackFromCheerio($, siteName) {
    $('script, style, noscript').remove();

    return {
        title: $('title').first().text() || null,
        textContent: $('body').text().replace(/\s+/g, ' ').trim() || null,
        byline:
            $('meta[name="author"]').attr('content') ||
            $('meta[property="article:author"]').attr('content') ||
            null,
        excerpt:
            $('meta[name="description"]').attr('content') ||
            $('meta[property="og:description"]').attr('content') ||
            null,
        siteName
    };
}

async function extractFromHtml(html, url) {
    const siteName = url ? new URL(url).hostname.replace(/^www\./, '') : null;
    const $ = cheerio.load(html);

    let result = null;

    try {
        const { document } = parseHTML(html);
        const parsed = await Defuddle(document, url, { markdown: true });

        if (parsed?.content) {
            result = {
                title: parsed.title ?? null,
                textContent: parsed.content,
                byline: parsed.author ?? null,
                excerpt: parsed.description ?? null,
                siteName
            };
        }
    } catch (err) {
        console.warn(`Defuddle parse failed for ${url}: ${err.message}`);
    }

    return result ?? fallbackFromCheerio($, siteName);
}

async function extractFromUrl(url) {
    const browser = await getBrowser();
    const page = await browser.newPage();

    try {
        await page.goto(url, { waitUntil: 'networkidle', timeout: NAVIGATION_TIMEOUT_MS });
        const html = await page.content();
        return await extractFromHtml(html, url);
    } catch (err) {
        console.warn(`Extraction failed for ${url}: ${err.message}`);
        return emptyResult;
    } finally {
        await page.close();
    }
}

// Own timeout so there is no wait to get anything hanging
function withTimeout(promise) {
    return Promise.race([
        promise,
        new Promise((resolve) => setTimeout(() => resolve(emptyResult), REQUEST_TIMEOUT_MS))
    ]);
}

// Set up Express app and endpoints
const app = express();
app.use(express.json({ limit: '50mb' }));

app.get('/health', (_req, res) => res.sendStatus(200));

app.post('/extract', async (req, res) => {
    const { url, html } = req.body ?? {};

    if (html && typeof html === 'string') {
        return res.json(await withTimeout(extractFromHtml(html, url)));
    }

    if (url && typeof url === 'string') {
        return res.json(await withTimeout(extractFromUrl(url)));
    }

    res.status(400).json({ error: 'Provide either "url" or "html" in the request body.' });
});

app.listen(PORT, () => console.log(`Webscrape service listening on port ${PORT}`));