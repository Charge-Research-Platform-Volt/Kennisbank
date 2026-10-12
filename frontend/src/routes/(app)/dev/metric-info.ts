// Explanations shown in the dashboard's info tooltips, keyed by the metric names the backend records

const TOOL_PREFIX = 'chat.tool.';

const TOOL_INFO: Record<string, string> = {
	search_library: 'Searches the library for resources, people or organisations.',
	get_item_details: 'Looks up the full details of one library item.',
	find_related_items: 'Finds items linked to an item, such as all resources by a person.',
	search_item_content: 'Searches inside the full text of one specific item for more detail.',
	browse_library:
		'Filters resources by tags, regions, types, journals or dates instead of searching on text.',
	find_similar_resources: 'Finds resources whose content is similar to a given resource.',
	search_attachment_content: 'Searches inside a file attached to the chat.'
};

export const METRIC_INFO: Record<string, string> = {
	'activity.chats':
		'Chats started per day, counted from the stored chats. Available for the past as well.',
	'activity.user_messages':
		'Messages users sent to the chatbot, counted from the stored chat messages. The chatbot’s answers are not included.',
	'activity.resources': 'Resources added per day, counted from the stored resources.',
	'search.content':
		'Searches on the Search page. Only the first page counts, loading more results doesn’t count again. The duration is how long the backend took to search.',
	'search.zero_results':
		'Searches that found nothing at all. Handy for spotting what the library is missing.',
	'files.upload':
		'Completed file uploads, both files for new resources and files attached to a chat.',
	'chat.response':
		'Chatbot answers, timed from sending the message until the full answer is done, including tool calls. Failed means the user got an error message or an empty answer. Answers cancelled mid-stream aren’t counted.',
	'chat.tool.unknown':
		'Tool calls with a name the app doesn’t know, which means the model made one up.',
	'document.extraction.file':
		'Processing an uploaded file after adding a resource: reading the document plus AI metadata extraction. Timed on the server from job start until the metadata is ready, so upload time and the polling delay are excluded.',
	'document.extraction.url':
		'Processing a link to a document (e.g. a PDF URL) after adding a resource: downloading and reading it plus AI metadata extraction. Timed on the server from job start until the metadata is ready.',
	'document.extraction.web':
		'Processing a webpage after adding a resource: scraping the page plus AI metadata extraction. Timed on the server from job start until the metadata is ready.',
	'metadata.ai':
		'The AI part of document processing: extracting title, authors and other metadata from the text. Failed means no metadata came back.',
	'ocr.document':
		'Documents (PDFs and images) read by Mistral OCR. Results reused from the cache are not included.',
	'ocr.pages': 'Pages read by Mistral OCR. Mistral bills OCR per page.',
	'ocr.cache_hit':
		'Times OCR was skipped because a stored result for the same file and OCR model already existed. Changing the OCR model makes old results stale.',
	'webscrape.page':
		'Webpages fetched and read by the webscrape service. Failed means it returned no text.',
	'mistral.request':
		'All calls to the Mistral API: chat, OCR, file uploads and the model list check. For streamed answers the duration only runs until Mistral starts responding. Failed means it still failed after retries.',
	'mistral.retry':
		'Times a Mistral call was tried again after a rate limit, overload or connection error.',
	'mistral.rate_limited': 'Responses where Mistral said too many requests were sent (HTTP 429).',
	'http.request':
		'API calls handled by the backend. The dashboard itself and the SignalR connection (used by the chat) are left out. Failed means a server error (5xx). 4xx responses like 401 or 404 count as successful.'
};

export function describeMetric(key: string): string | null {
	if (key in METRIC_INFO) return METRIC_INFO[key];

	if (key.startsWith(TOOL_PREFIX)) {
		const tool = key.slice(TOOL_PREFIX.length);
		const what = TOOL_INFO[tool];
		return `How often the chatbot used the ${tool} tool while answering.${what ? ` ${what}` : ''} Failed means the tool threw an error.`;
	}

	return null;
}
