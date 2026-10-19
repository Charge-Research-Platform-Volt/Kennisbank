<script lang="ts">
	import { api } from '$lib/api';
	import { onMount } from 'svelte';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Textarea } from '$lib/components/ui/textarea';
	import { confirm } from '$lib/state/confirm.svelte';
	import { RefreshCw, Trash, X } from '@lucide/svelte';
	import { toast } from 'svelte-sonner';

	type RunStatus = 'Running' | 'Completed' | 'Cancelled' | 'Failed' | 'Interrupted';

	type RunSummary = {
		id: string;
		startedAt: string;
		completedAt: string | null;
		model: string;
		label: string | null;
		status: RunStatus;
		casesTotal: number;
		casesDone: number;
		turns: number;
		failedTurns: number;
		avgTotalMs: number | null;
		avgFirstTokenMs: number | null;
		avgRounds: number | null;
		avgToolCalls: number | null;
		promptTokens: number;
		completionTokens: number;
		citations: number;
		unsupportedCitations: number;
	};

	type TurnResult = {
		question: string;
		answer: string;
		error: string | null;
		rounds: number;
		toolCalls: number;
		firstTokenMs: number | null;
		finalRoundMs: number;
		totalMs: number;
		promptTokens: number;
		completionTokens: number;
		citations: number;
		unsupportedCitations: string[];
		toolResults: string[];
	};

	type RunDetail = { summary: RunSummary; cases: { id: string; turns: TurnResult[] }[] };

	type EvalCase = { id: string; turns: string[] };

	let runs = $state<RunSummary[] | null>(null);
	let selected = $state<RunDetail | null>(null);
	let model = $state('');
	let label = $state('');
	let casesJson = $state('');
	let submitting = $state(false);

	let running = $derived(runs?.some((r) => r.status === 'Running') ?? false);

	// Only catches what's obvious while typing; the backend checks the rest (ids, empty turns) when the run starts
	let parsedCases = $derived.by((): { cases: EvalCase[] | null; error: string | null } => {
		if (!casesJson.trim()) return { cases: null, error: 'Paste the cases to run.' };
		try {
			const value: unknown = JSON.parse(casesJson);
			if (!Array.isArray(value) || value.length === 0)
				return { cases: null, error: 'Expected a non-empty array of cases.' };
			return { cases: value as EvalCase[], error: null };
		} catch (e) {
			return { cases: null, error: e instanceof Error ? e.message : 'Invalid JSON.' };
		}
	});

	// Starts from the questions of the latest run, so running again right after compares like with like
	async function loadLatestCases() {
		try {
			const cases = await api.get<EvalCase[]>('/api/admin/chat-eval/cases/latest');
			casesJson = JSON.stringify(cases, null, 2);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to load the latest cases.');
		}
	}

	async function fetchRuns() {
		try {
			runs = await api.get<RunSummary[]>('/api/admin/chat-eval/runs');
			// Keep an open run's details in step while it's still filling in
			if (selected?.summary.status === 'Running') await openRun(selected.summary.id);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to load the chat eval runs.');
		}
	}

	async function openRun(id: string) {
		selected = await api.get<RunDetail>(`/api/admin/chat-eval/runs/${id}`);
	}

	async function startRun() {
		if (!parsedCases.cases) return;

		submitting = true;
		try {
			await api.post('/api/admin/chat-eval/run', {
				model: model.trim() || null,
				label: label.trim() || null,
				cases: parsedCases.cases
			});
			toast.success('Chat eval started');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to start the chat eval.');
		} finally {
			submitting = false;
			await fetchRuns();
		}
	}

	async function cancelRun() {
		submitting = true;
		try {
			await api.post('/api/admin/chat-eval/cancel');
			toast.success('Cancelling...');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to cancel.');
		} finally {
			submitting = false;
			await fetchRuns();
		}
	}

	async function deleteRun(run: RunSummary) {
		const name = run.label ?? run.model;
		if (!(await confirm(`Delete the run "${name}" from ${formatDate(run.startedAt)}?`))) return;

		try {
			await api.delete(`/api/admin/chat-eval/runs/${run.id}`);
			if (selected?.summary.id === run.id) selected = null;
			await fetchRuns();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete the run.');
		}
	}

	// Every finished case is saved right away, so polling shows progress per case
	$effect(() => {
		if (!running) return;
		const timer = setInterval(fetchRuns, 5000);
		return () => clearInterval(timer);
	});

	onMount(() => {
		fetchRuns();
		loadLatestCases();
	});

	const formatDate = (iso: string) => new Date(iso).toLocaleString();
	const seconds = (ms: number | null) => (ms == null ? '—' : `${(ms / 1000).toFixed(1)} s`);
	const decimal = (n: number | null) => (n == null ? '—' : n.toFixed(1));

	const STATUS_CLASS: Record<RunStatus, string> = {
		Running: 'text-primary',
		Completed: '',
		Cancelled: 'text-muted-foreground',
		Failed: 'text-destructive',
		Interrupted: 'text-destructive'
	};
</script>

<div class="mx-auto flex h-full w-full max-w-4xl flex-col gap-6 overflow-y-auto p-8 xl:max-w-7xl">
	<div class="flex items-center justify-between">
		<h1 class="text-lg font-semibold">Chat eval</h1>
		<button
			onclick={fetchRuns}
			class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
			title="Refresh"
		>
			<RefreshCw size={16} />
		</button>
	</div>

	<p class="text-sm text-muted-foreground">
		Runs the cases below through the real chat, one after another, against the live library. Nothing
		is saved as a chat. Runs are only comparable with the same cases on the same library, so compare
		runs done shortly after each other. Unsupported citations are sources an answer cites that none
		of its tool calls returned.
	</p>

	<!-- Cases to run -->
	<label class="flex flex-col gap-1 text-sm">
		<span class="flex items-center justify-between text-muted-foreground">
			Cases: an array of {'{ "id": "...", "turns": ["question", "follow-up"] }'}
			<button
				onclick={loadLatestCases}
				disabled={running}
				class="cursor-pointer text-xs underline hover:text-foreground disabled:cursor-default disabled:opacity-40"
			>
				Reset to latest run's cases
			</button>
		</span>
		<Textarea
			bind:value={casesJson}
			disabled={running}
			spellcheck={false}
			class="h-64 font-mono text-xs"
		/>
		{#if parsedCases.error && casesJson.trim()}
			<span class="text-xs text-destructive">{parsedCases.error}</span>
		{:else if parsedCases.cases}
			<span class="text-xs text-muted-foreground">
				{parsedCases.cases.length} cases, {parsedCases.cases.reduce(
					(n, c) => n + (Array.isArray(c.turns) ? c.turns.length : 0),
					0
				)} turns
			</span>
		{/if}
	</label>

	<!-- Start a run -->
	<div class="flex flex-wrap items-end gap-2">
		<label class="flex flex-col gap-1 text-sm">
			<span class="text-muted-foreground">Model (empty = the chat's current model)</span>
			<Input
				bind:value={model}
				placeholder="e.g. mistral-large-4"
				disabled={running}
				class="w-64"
			/>
		</label>
		<label class="flex flex-col gap-1 text-sm">
			<span class="text-muted-foreground">Label</span>
			<Input bind:value={label} placeholder="e.g. baseline" disabled={running} class="w-64" />
		</label>

		{#if running}
			<Button
				variant="destructive"
				onclick={cancelRun}
				disabled={submitting}
				class="cursor-pointer"
			>
				Cancel run
			</Button>
		{:else}
			<Button onclick={startRun} disabled={submitting || !parsedCases.cases} class="cursor-pointer"
				>Run eval</Button
			>
		{/if}
	</div>

	<!-- Runs -->
	{#if !runs}
		<div class="flex justify-center p-8">
			<Spinner class="h-6 w-6" />
		</div>
	{:else if runs.length === 0}
		<span class="text-sm text-muted-foreground">No runs yet.</span>
	{:else}
		<div class="overflow-x-auto rounded-md border">
			<table class="w-full text-sm">
				<thead class="text-left text-muted-foreground">
					<tr>
						<th class="p-2 font-medium">Started</th>
						<th class="p-2 font-medium">Label</th>
						<th class="p-2 font-medium">Model</th>
						<th class="p-2 font-medium">Status</th>
						<th class="p-2 text-right font-medium">Avg time</th>
						<th class="p-2 text-right font-medium">First text</th>
						<th class="p-2 text-right font-medium">Rounds</th>
						<th class="p-2 text-right font-medium">Tool calls</th>
						<th class="p-2 text-right font-medium">Tokens in / out</th>
						<th class="p-2 text-right font-medium">Citations</th>
						<th class="p-2 text-right font-medium">Failed turns</th>
						<th class="p-2"></th>
					</tr>
				</thead>
				<tbody>
					{#each runs as run (run.id)}
						<tr
							class="cursor-pointer border-t hover:bg-accent/50 {selected?.summary.id === run.id
								? 'bg-accent/50'
								: ''}"
							onclick={() =>
								selected?.summary.id === run.id ? (selected = null) : openRun(run.id)}
						>
							<td class="p-2 whitespace-nowrap">{formatDate(run.startedAt)}</td>
							<td class="p-2">{run.label ?? '—'}</td>
							<td class="p-2 font-mono text-xs">{run.model}</td>
							<td class="p-2 whitespace-nowrap {STATUS_CLASS[run.status]}">
								{run.status}{#if run.status === 'Running' || run.casesDone < run.casesTotal}
									<span class="text-muted-foreground"> · {run.casesDone}/{run.casesTotal}</span>
								{/if}
							</td>
							<td class="p-2 text-right">{seconds(run.avgTotalMs)}</td>
							<td class="p-2 text-right">{seconds(run.avgFirstTokenMs)}</td>
							<td class="p-2 text-right">{decimal(run.avgRounds)}</td>
							<td class="p-2 text-right">{decimal(run.avgToolCalls)}</td>
							<td class="p-2 text-right whitespace-nowrap">
								{run.promptTokens.toLocaleString()} / {run.completionTokens.toLocaleString()}
							</td>
							<td class="p-2 text-right whitespace-nowrap">
								{run.citations}{#if run.unsupportedCitations > 0}
									<span class="text-destructive"> · {run.unsupportedCitations} unsupported</span>
								{/if}
							</td>
							<td class="p-2 text-right {run.failedTurns > 0 ? 'text-destructive' : ''}">
								{run.failedTurns}
							</td>
							<td class="p-2 text-right">
								<button
									onclick={(e) => {
										e.stopPropagation();
										deleteRun(run);
									}}
									disabled={run.status === 'Running'}
									class="cursor-pointer rounded-md p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-destructive disabled:cursor-default disabled:opacity-40"
									title="Delete run"
								>
									<Trash size={14} />
								</button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}

	<!-- Details of the selected run -->
	{#if selected}
		<div class="flex flex-col gap-4">
			<div class="flex items-center justify-between">
				<h2 class="font-semibold">
					{selected.summary.label ?? 'Unlabelled run'}
					<span class="text-sm font-normal text-muted-foreground">
						· <span class="font-mono text-xs">{selected.summary.model}</span> · {formatDate(
							selected.summary.startedAt
						)}
					</span>
				</h2>
				<button
					onclick={() => (selected = null)}
					class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
					title="Close details"
				>
					<X size={16} />
				</button>
			</div>

			{#if selected.cases.length === 0}
				<span class="text-sm text-muted-foreground">No finished cases yet.</span>
			{/if}

			{#each selected.cases as evalCase (evalCase.id)}
				<div class="flex flex-col divide-y rounded-md border">
					<span class="px-4 py-2 font-mono text-xs text-muted-foreground">{evalCase.id}</span>

					{#each evalCase.turns as turn, i (i)}
						<div class="flex flex-col gap-2 p-4">
							<span class="text-sm font-medium">{turn.question}</span>

							<span class="text-xs text-muted-foreground">
								{seconds(turn.totalMs)} total · first text {seconds(turn.firstTokenMs)} · final round
								{seconds(turn.finalRoundMs)} · {turn.rounds} rounds · {turn.toolCalls} tool calls · {turn.promptTokens.toLocaleString()}
								/ {turn.completionTokens.toLocaleString()} tokens · {turn.citations} citations
							</span>

							{#if turn.error}
								<span class="text-sm text-destructive">{turn.error}</span>
							{/if}

							{#if turn.unsupportedCitations.length > 0}
								<span class="text-xs text-destructive">
									Unsupported citations:
									<span class="font-mono">{turn.unsupportedCitations.join(', ')}</span>
								</span>
							{/if}

							{#if turn.answer}
								<pre
									class="rounded-md bg-muted/50 p-3 font-sans text-sm whitespace-pre-wrap">{turn.answer}</pre>
							{/if}

							{#if turn.toolResults.length > 0}
								<details class="text-sm">
									<summary class="cursor-pointer text-muted-foreground">
										Tool results ({turn.toolResults.length})
									</summary>
									<div class="mt-2 flex flex-col gap-2">
										{#each turn.toolResults as result, j (j)}
											<pre
												class="max-h-80 overflow-y-auto rounded-md bg-muted/50 p-3 text-xs whitespace-pre-wrap">{result}</pre>
										{/each}
									</div>
								</details>
							{/if}
						</div>
					{/each}
				</div>
			{/each}
		</div>
	{/if}
</div>
