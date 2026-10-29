<script lang="ts">
	import { api } from '#lib/api.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import Spinner from '#lib/components/ui/spinner/spinner.svelte';
	import UsageChart from './UsageChart.svelte';
	import InfoTip from './InfoTip.svelte';
	import { describeMetric, METRIC_INFO } from './metric-info';

	type UsageSeries = {
		key: string;
		counts: number[];
		failures: number[];
		avgDurationMs: (number | null)[];
	};

	type UsageResponse = { dates: string[]; series: UsageSeries[] };

	type Tile = { label: string; counts: number[]; detail: string | null; info: string };

	const RANGES = [7, 30, 90] as const;
	const TOOL_PREFIX = 'chat.tool.';

	let days = $state<number>(30);
	let usage = $state<UsageResponse | null>(null);
	let error = $state<string | null>(null);

	$effect(() => {
		const requested = days;
		api
			.get<UsageResponse>(`/api/dashboard/usage?days=${requested}`)
			.then((res) => {
				// Ignore a slower response for a range that's no longer selected
				if (requested !== days) return;
				usage = res;
				error = null;
			})
			.catch((e) => {
				error = e instanceof Error ? e.message : 'Failed to load usage.';
			});
	});

	function series(key: string): UsageSeries {
		const found = usage?.series.find((s) => s.key === key);
		if (found) return found;

		const zeros = (usage?.dates ?? []).map(() => 0);
		return { key, counts: zeros, failures: zeros, avgDurationMs: zeros.map(() => null) };
	}

	const sum = (values: number[]) => values.reduce((a, b) => a + b, 0);

	// Average over the whole range, weighted by how many happened each day
	function avgMs(s: UsageSeries): number | null {
		let totalMs = 0;
		let count = 0;
		s.avgDurationMs.forEach((avg, i) => {
			if (avg == null) return;
			totalMs += avg * s.counts[i];
			count += s.counts[i];
		});
		return count > 0 ? totalMs / count : null;
	}

	function formatMs(ms: number | null) {
		if (ms == null) return null;
		return ms < 1000 ? `avg ${Math.round(ms)} ms` : `avg ${(ms / 1000).toFixed(1)} s`;
	}

	function failed(s: UsageSeries) {
		const count = sum(s.failures);
		return count > 0 ? `${count.toLocaleString()} failed` : null;
	}

	function detail(...parts: (string | null)[]) {
		return parts.filter((p) => p != null).join(' · ') || null;
	}

	// Adds series together per day, keeping the duration average weighted by count
	function combine(key: string, parts: UsageSeries[]): UsageSeries {
		const dates = usage?.dates ?? [];
		const counts = dates.map((_, i) => sum(parts.map((p) => p.counts[i])));
		const avgDurationMs = dates.map((_, i) => {
			const timed = parts.filter((p) => p.avgDurationMs[i] != null);
			const count = sum(timed.map((p) => p.counts[i]));
			return count > 0 ? sum(timed.map((p) => p.avgDurationMs[i]! * p.counts[i])) / count : null;
		});
		return {
			key,
			counts,
			failures: dates.map((_, i) => sum(parts.map((p) => p.failures[i]))),
			avgDurationMs
		};
	}

	function totalMs(s: UsageSeries) {
		return sum(s.avgDurationMs.map((avg, i) => (avg ?? 0) * s.counts[i]));
	}

	const DOCUMENT_SOURCES = [
		{ key: 'document.extraction.file', label: 'Uploaded file' },
		{ key: 'document.extraction.url', label: 'Document link' },
		{ key: 'document.extraction.web', label: 'Webpage' }
	];

	let groups = $derived.by((): { title: string; info: string; tiles: Tile[] }[] => {
		if (!usage) return [];

		const search = series('search.content');
		const chat = series('chat.response');
		const firstToken = series('chat.first_token');
		const finalRound = series('chat.final_round');
		const rounds = series('chat.rounds');
		// Only answers on days that recorded rounds, so days from before these metrics existed don't dilute the average
		const answersWithRounds = sum(chat.counts.filter((_, i) => rounds.counts[i] > 0));
		const perAnswer = (s: UsageSeries, label: string) =>
			answersWithRounds > 0
				? `avg ${(sum(s.counts) / answersWithRounds).toFixed(1)} ${label}`
				: null;
		const ocr = series('ocr.document');
		const ocrPages = sum(series('ocr.pages').counts);
		const documents = combine(
			'document.extraction',
			DOCUMENT_SOURCES.map((s) => series(s.key))
		);
		const metadataAi = series('metadata.ai');
		const webscrape = series('webscrape.page');
		const mistral = series('mistral.request');
		const http = series('http.request');
		const requests = sum(http.counts);
		const serverErrors = sum(http.failures);

		return [
			{
				title: 'Activity',
				info: 'What people do in the app.',
				tiles: [
					{
						label: 'Chats started',
						counts: series('activity.chats').counts,
						detail: null,
						info: METRIC_INFO['activity.chats']
					},
					{
						label: 'Chat messages sent',
						counts: series('activity.user_messages').counts,
						detail: null,
						info: METRIC_INFO['activity.user_messages']
					},
					{
						label: 'Searches',
						counts: search.counts,
						detail: detail(
							`${sum(series('search.zero_results').counts).toLocaleString()} without results`,
							formatMs(avgMs(search))
						),
						info: `${METRIC_INFO['search.content']} “Without results” are searches that found nothing at all.`
					},
					{
						label: 'Resources added',
						counts: series('activity.resources').counts,
						detail: null,
						info: METRIC_INFO['activity.resources']
					},
					{
						label: 'Files uploaded',
						counts: series('files.upload').counts,
						detail: null,
						info: METRIC_INFO['files.upload']
					}
				]
			},
			{
				title: 'Documents',
				info: 'Adding resources: how long reading a document and extracting its metadata takes. Embedding is left out.',
				tiles: [
					{
						label: 'Documents processed',
						counts: documents.counts,
						detail: detail(failed(documents), formatMs(avgMs(documents))),
						info: 'The wait after adding a resource until its metadata is ready, for files, document links and webpages together. Timed on the server from job start until the metadata is ready, so upload time and the polling delay are excluded. The breakdown by source is further down.'
					},
					{
						label: 'AI metadata extraction',
						counts: metadataAi.counts,
						detail: detail(failed(metadataAi), formatMs(avgMs(metadataAi))),
						info: METRIC_INFO['metadata.ai']
					},
					{
						label: 'OCR documents',
						counts: ocr.counts,
						detail: detail(
							`${ocrPages.toLocaleString()} pages`,
							ocrPages > 0
								? (formatMs(totalMs(ocr) / ocrPages)?.replace('avg', 'avg/page') ?? null)
								: null,
							failed(ocr),
							`${sum(series('ocr.cache_hit').counts).toLocaleString()} cache hits`
						),
						info: `${METRIC_INFO['ocr.document']} Avg/page is the total OCR time divided by the number of pages. Cache hits are files where a stored OCR result was reused instead.`
					},
					{
						label: 'Webpages scraped',
						counts: webscrape.counts,
						detail: detail(failed(webscrape), formatMs(avgMs(webscrape))),
						info: METRIC_INFO['webscrape.page']
					}
				]
			},
			{
				title: 'AI',
				info: 'Chatbot answers and the Mistral calls behind them.',
				tiles: [
					{
						label: 'Chat responses',
						counts: chat.counts,
						detail: detail(
							failed(chat),
							formatMs(avgMs(chat)),
							perAnswer(rounds, 'rounds'),
							perAnswer(series('chat.tool_calls'), 'tool calls')
						),
						info: `${METRIC_INFO['chat.response']} Rounds and tool calls are averaged per answer.`
					},
					{
						label: 'Time to first answer text',
						counts: firstToken.counts,
						detail: detail(
							formatMs(avgMs(firstToken)),
							formatMs(avgMs(finalRound))?.replace('avg', 'final round') ?? null
						),
						info: `${METRIC_INFO['chat.first_token']} Final round: ${METRIC_INFO['chat.final_round']}`
					},
					{
						label: 'Mistral requests',
						counts: mistral.counts,
						detail: detail(
							failed(mistral),
							`${sum(series('mistral.retry').counts).toLocaleString()} retries`,
							`${sum(series('mistral.rate_limited').counts).toLocaleString()} rate limited`
						),
						info: `${METRIC_INFO['mistral.request']} Retries and rate limits are counted per attempt.`
					}
				]
			},
			{
				title: 'Health',
				info: 'Whether the backend handles requests without errors.',
				tiles: [
					{
						label: 'API requests',
						counts: http.counts,
						detail: null,
						info: METRIC_INFO['http.request']
					},
					{
						label: 'Server errors (5xx)',
						counts: http.failures,
						detail:
							requests > 0 ? `${((serverErrors / requests) * 100).toFixed(2)}% of requests` : null,
						info: 'API calls that ended in a server error (HTTP 5xx), and their share of all API calls. 4xx responses like 401 or 404 are not counted as errors.'
					}
				]
			}
		];
	});

	type BreakdownRow = {
		name: string;
		total: number;
		failed: number;
		avg: string | null;
		info: string | null;
	};

	function toRow(name: string, s: UsageSeries): BreakdownRow {
		return {
			name,
			total: sum(s.counts),
			failed: sum(s.failures),
			avg: formatMs(avgMs(s)),
			info: describeMetric(s.key)
		};
	}

	let tools = $derived(
		(usage?.series ?? [])
			.filter((s) => s.key.startsWith(TOOL_PREFIX))
			.map((s) => toRow(s.key.slice(TOOL_PREFIX.length), s))
			.sort((a, b) => b.total - a.total)
	);

	let documentSources = $derived(
		usage ? DOCUMENT_SOURCES.map((s) => toRow(s.label, series(s.key))) : []
	);
</script>

{#snippet breakdown(
	title: string,
	info: string,
	rows: BreakdownRow[],
	emptyText: string,
	mono: boolean
)}
	{@const max = Math.max(1, ...rows.map((r) => r.total))}
	<div class="flex flex-col gap-2 rounded-md border p-4">
		<span class="flex items-center gap-1.5 text-sm text-muted-foreground"
			>{title}<InfoTip text={info} /></span
		>

		{#if rows.every((r) => r.total === 0)}
			<span class="text-sm text-muted-foreground">{emptyText}</span>
		{:else}
			{#each rows as row (row.name)}
				<div class="grid grid-cols-[10rem_1fr_auto] items-center gap-3 text-sm">
					<span class="flex min-w-0 items-center gap-1.5">
						<span class="truncate {mono ? 'font-mono text-xs' : ''}">{row.name}</span>
						<InfoTip text={row.info} />
					</span>
					<div class="h-2">
						<div
							class="h-full rounded-r bg-primary"
							style="width: {(row.total / max) * 100}%"
						></div>
					</div>
					<span class="text-right text-xs text-muted-foreground">
						{row.total.toLocaleString()}{#if row.failed > 0}
							· {row.failed} failed{/if}{#if row.avg}
							· {row.avg}{/if}
					</span>
				</div>
			{/each}
		{/if}
	</div>
{/snippet}

<div class="flex flex-col gap-4">
	<div class="flex items-center justify-between">
		<h2 class="flex items-center gap-1.5 font-semibold">
			Usage <span class="text-sm font-normal text-muted-foreground"
				>· daily totals (UTC), no per-user data</span
			>
			<InfoTip
				text="Only how often something happened per day, never who did it. Days run from midnight to midnight UTC. New numbers can take up to a minute to show up."
			/>
		</h2>

		<div class="flex gap-1">
			{#each RANGES as range (range)}
				<Button
					size="sm"
					variant={days === range ? 'secondary' : 'ghost'}
					onclick={() => (days = range)}
				>
					{range} days
				</Button>
			{/each}
		</div>
	</div>

	{#if error}
		<span class="text-sm text-destructive">{error}</span>
	{/if}

	{#if !usage}
		<div class="flex justify-center p-8">
			<Spinner class="h-6 w-6" />
		</div>
	{:else}
		{#each groups as group (group.title)}
			<div class="flex flex-col gap-2">
				<span class="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
					{group.title}<InfoTip text={group.info} />
				</span>
				<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
					{#each group.tiles as tile (tile.label)}
						<UsageChart
							label={tile.label}
							dates={usage.dates}
							counts={tile.counts}
							detail={tile.detail}
							info={tile.info}
						/>
					{/each}
				</div>
			</div>
		{/each}

		<div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
			{@render breakdown(
				'Document processing by source',
				'Documents processed, split by where the resource came from. The source makes a big difference: OCR on an uploaded PDF takes a lot longer than reading a webpage.',
				documentSources,
				'Nothing processed in this period.',
				false
			)}
			{@render breakdown(
				'Chatbot tool calls',
				'How often the chatbot used each tool to look things up while answering, most used first.',
				tools,
				'No tool calls in this period.',
				true
			)}
		</div>

		<div class="rounded-md border p-4 text-sm">
			<span class="flex items-center gap-1.5 text-muted-foreground">
				All totals for this period
				<InfoTip
					text="Every recorded metric under its raw name, added up over the selected period."
				/>
			</span>

			<table class="mt-3 w-full">
				<thead class="text-left text-muted-foreground">
					<tr>
						<th class="py-1 font-medium">Metric</th>
						<th class="py-1 text-right font-medium">Total</th>
						<th class="py-1 text-right font-medium">Failed</th>
						<th class="py-1 text-right font-medium">
							<span class="inline-flex items-center gap-1.5">
								Avg duration
								<InfoTip
									text="Average time per occurrence over the period. Empty for metrics that aren't timed."
								/>
							</span>
						</th>
					</tr>
				</thead>
				<tbody>
					{#each usage.series as s (s.key)}
						<tr class="border-t">
							<td class="py-1">
								<span class="inline-flex items-center gap-1.5 font-mono text-xs"
									>{s.key}<InfoTip text={describeMetric(s.key)} /></span
								>
							</td>
							<td class="py-1 text-right">{sum(s.counts).toLocaleString()}</td>
							<td class="py-1 text-right">{sum(s.failures).toLocaleString()}</td>
							<td class="py-1 text-right">{formatMs(avgMs(s))?.replace('avg ', '') ?? '—'}</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>
