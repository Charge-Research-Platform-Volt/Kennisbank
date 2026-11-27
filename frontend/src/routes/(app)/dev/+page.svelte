<script lang="ts">
	import { api } from '#lib/api.js';
	import { onMount } from 'svelte';
	import Spinner from '#lib/components/ui/spinner/spinner.svelte';
	import { CircleCheck, RefreshCw, TriangleAlert } from '@lucide/svelte';
	import { Button } from '#lib/components/ui/button/index.js';
	import UsageSection from './UsageSection.svelte';
	import InfoTip from './InfoTip.svelte';

	type LiveStatus = {
		connectedUsers: number;
		activeUsers: number;
		activeWindowMinutes: number;
		activeChatStreams: number;
		queue: { pending: number; running: number };
		reembedRunning: boolean;
		extractionJobs: number;
		safeToDeploy: boolean;
	};

	type MemoryStatus = {
		uptimeSeconds: number;
		processBytes: number;
		gcCommittedBytes: number;
		gcHeapBytes: number;
		gcFragmentedBytes: number;
		largeObjectHeapBytes: number;
		liveAfterLastFullGcBytes: number | null;
		fullCollections: number;
		gcBudgetBytes: number;
		serverGc: boolean;
	};

	const mib = (bytes: number) => `${Math.round(bytes / 1048576).toLocaleString()} MiB`;
	const gib = (bytes: number) => `${(bytes / 1073741824).toFixed(1)} GiB`;

	function formatUptime(seconds: number) {
		const days = Math.floor(seconds / 86400);
		const hours = Math.floor((seconds % 86400) / 3600);
		return days > 0 ? `${days}d ${hours}h` : `${hours}h ${Math.floor((seconds % 3600) / 60)}m`;
	}

	type ModelFamilyStatus = {
		name: string;
		configured: string | null;
		usedByApp: boolean;
		status: 'UpToDate' | 'UpdateAvailable' | 'NotPinned' | 'Missing' | 'WatchOnly' | 'Unknown';
		latest: string | null;
		deprecation: string | null;
		deprecationReplacement: string | null;
		models: { names: string[]; isNew: boolean; isLatest: boolean; isConfigured: boolean }[];
	};

	type ModelsResponse = {
		checkedAt: string | null;
		error: string | null;
		families: ModelFamilyStatus[];
	};

	const STATUS_LABEL: Record<ModelFamilyStatus['status'], string> = {
		UpToDate: 'Up to date',
		UpdateAvailable: 'Update available',
		NotPinned: 'Not pinned (-latest)',
		Missing: 'Pinned model no longer listed',
		WatchOnly: 'Watching',
		Unknown: 'Unknown'
	};

	const STATUS_INFO: Record<ModelFamilyStatus['status'], string> = {
		UpToDate: 'The pinned model is the one Mistral’s -latest alias currently points to.',
		UpdateAvailable:
			'Mistral’s -latest alias now points to a newer model than the pinned one. Test the new one, then update the environment variable (or the reference in code for Large).',
		NotPinned:
			'Configured as -latest, so Mistral can swap the model without notice. Pin a dated model id instead.',
		Missing:
			'The pinned model isn’t in Mistral’s model list anymore, so requests to it may start failing. Pin a model that is listed.',
		WatchOnly: 'Not used by the app, only listed to see what’s available.',
		Unknown:
			'Mistral lists no -latest alias for this family, so there’s nothing to compare against.'
	};

	const NEEDS_ATTENTION: ModelFamilyStatus['status'][] = [
		'UpdateAvailable',
		'Missing',
		'NotPinned'
	];

	let live = $state<LiveStatus | null>(null);
	let models = $state<ModelsResponse | null>(null);
	let modelsBusy = $state(false);
	let hasNewModels = $derived(models?.families.some((f) => f.models.some((m) => m.isNew)) ?? false);

	async function modelsAction(request: () => Promise<ModelsResponse>) {
		modelsBusy = true;
		try {
			models = await request();
		} finally {
			modelsBusy = false;
		}
	}

	let memory = $state<MemoryStatus | null>(null);
	let collecting = $state(false);
	// Process size just before the last forced collection, to show what it freed
	let processBeforeCollect = $state<number | null>(null);

	async function fetchLive() {
		try {
			live = await api.get<LiveStatus>('/api/dashboard/live');
		} catch {
			// Keep showing last known values
		}
	}

	async function fetchMemory() {
		try {
			memory = await api.get<MemoryStatus>('/api/dashboard/memory');
		} catch {
			// Keep showing last known values
		}
	}

	async function collectMemory() {
		collecting = true;
		try {
			processBeforeCollect = memory?.processBytes ?? null;
			memory = await api.post<MemoryStatus>('/api/dashboard/memory/collect');
		} finally {
			collecting = false;
		}
	}

	onMount(() => {
		fetchLive();
		fetchMemory();
		modelsAction(() => api.get<ModelsResponse>('/api/dashboard/models'));
		const interval = setInterval(() => {
			fetchLive();
			fetchMemory();
		}, 10_000);
		return () => clearInterval(interval);
	});
</script>

<div class="mx-auto flex h-full w-full max-w-4xl flex-col gap-6 overflow-y-auto p-8 xl:max-w-7xl">
	<div class="flex items-center justify-between">
		<h1 class="text-lg font-semibold">Dashboard</h1>

		<div class="flex gap-4 text-sm">
			<a href="/dev/new-changelog" class="text-muted-foreground underline hover:text-foreground">
				New changelog entry
			</a>
			<a href="/dev/reembed" class="text-muted-foreground underline hover:text-foreground">
				Embedding status
			</a>
			<a href="/dev/chat-eval" class="text-muted-foreground underline hover:text-foreground">
				Chat eval
			</a>
		</div>
	</div>

	<!-- Live status and model notices side by side on wide screens -->
	<div class="grid grid-cols-1 gap-6 xl:grid-cols-2 xl:items-start">
		<div class="flex flex-col gap-6">
			{#if !live}
				<div class="flex items-center justify-center p-8">
					<Spinner class="h-8 w-8" />
				</div>
			{:else}
				<div
					class="flex items-center gap-3 rounded-md border p-4 {live.safeToDeploy
						? 'border-green-600/40 text-green-700 dark:text-green-400'
						: 'border-orange-500/40 text-orange-600 dark:text-orange-400'}"
				>
					{#if live.safeToDeploy}
						<CircleCheck size={20} />
						<span class="font-medium">Safe to deploy, no work in progress</span>
					{:else}
						<TriangleAlert size={20} />
						<span class="font-medium">Work in progress, deploying now would interrupt it</span>
					{/if}
					<InfoTip
						text="Safe when no chat answer is streaming, the background queue is empty and idle, no re-embed is running and no metadata extraction is in progress. People who are only online don't block it, but check the active count before deploying. Updates every 10 seconds."
					/>
				</div>

				<div class="grid grid-cols-2 gap-4">
					<div class="flex flex-col gap-2 rounded-md border p-4">
						<span class="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
							Users (excluding you)
							<InfoTip text="Only counts, never who. All of your own tabs are left out." />
						</span>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Online<InfoTip
									text="People with the app open in at least one tab, including idle tabs left open."
								/>
							</span>
							<span class="font-mono">{live.connectedUsers}</span>
						</div>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Active in last {live.activeWindowMinutes} min
								<InfoTip
									text="Online people who did something recently, like opening a page, searching or chatting. A better sign than Online that someone is actually busy."
								/>
							</span>
							<span class="font-mono">{live.activeUsers}</span>
						</div>
					</div>

					<div class="flex flex-col gap-2 rounded-md border p-4">
						<span class="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
							Running work
							<InfoTip
								text="Work a deploy would cut off. Anything above zero turns the deploy indicator orange."
							/>
						</span>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Chat responses<InfoTip text="Chatbot answers being written right now." />
							</span>
							<span class="font-mono">{live.activeChatStreams}</span>
						</div>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Queue (running / pending)
								<InfoTip
									text="Background work such as embedding new resources. Running is being worked on now, pending is waiting its turn. A restart loses pending items; embeddings are retried automatically."
								/>
							</span>
							<span class="font-mono">{live.queue.running} / {live.queue.pending}</span>
						</div>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Metadata extraction jobs
								<InfoTip
									text="Someone is adding a resource and waiting for its metadata to be read from the document."
								/>
							</span>
							<span class="font-mono">{live.extractionJobs}</span>
						</div>

						<div class="flex items-center justify-between text-sm">
							<span class="flex items-center gap-1.5">
								Re-embed run<InfoTip text="A re-embed started from the Embedding status page." />
							</span>
							<span class="font-mono">{live.reembedRunning ? 'running' : 'idle'}</span>
						</div>
					</div>
				</div>
			{/if}

			{#if memory}
				<div class="flex flex-col gap-2 rounded-md border p-4">
					<div class="flex items-center justify-between">
						<span class="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
							Backend memory · up {formatUptime(memory.uptimeSeconds)}
							<InfoTip
								text="To tell a memory leak from a lazy garbage collector. With a leak, 'Still in use after a full collection' keeps climbing over days. If only the process grows while that stays flat, the garbage collector just isn't giving memory back yet. Updates every 10 seconds."
							/>
						</span>

						<Button
							size="sm"
							variant="outline"
							disabled={collecting}
							title="Force a full garbage collection"
							onclick={collectMemory}
						>
							{collecting ? 'Collecting…' : 'Collect now'}
						</Button>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							Process<InfoTip
								text="What the server sees, the same as docker stats: the .NET heap plus everything outside it."
							/>
						</span>
						<span class="font-mono">{mib(memory.processBytes)}</span>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							Still in use after a full collection<InfoTip
								text="What the app really holds on to. This is the number to watch over days: a leak makes it grow without stopping."
							/>
						</span>
						<span class="font-mono">
							{memory.liveAfterLastFullGcBytes === null
								? 'no full collection yet'
								: mib(memory.liveAfterLastFullGcBytes)}
						</span>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							.NET heap (fragmented)<InfoTip
								text="The managed heap now, including garbage not collected yet. Fragmented is free space inside it that can't be handed back until the heap is compacted."
							/>
						</span>
						<span class="font-mono">
							{mib(memory.gcHeapBytes)} ({mib(memory.gcFragmentedBytes)})
						</span>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							Large object heap<InfoTip
								text="Objects over 85 KB, such as whole files read into memory while processing. Only cleaned up by full collections."
							/>
						</span>
						<span class="font-mono">{mib(memory.largeObjectHeapBytes)}</span>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							Reserved by .NET<InfoTip
								text="Memory .NET has taken from the OS for its heap, used or not."
							/>
						</span>
						<span class="font-mono">{mib(memory.gcCommittedBytes)}</span>
					</div>

					<div class="flex items-center justify-between text-sm">
						<span class="flex items-center gap-1.5">
							Full collections · GC budget<InfoTip
								text="How often a full collection has run since startup, and how much memory the garbage collector thinks it may use: the whole server unless the container has a memory limit. A big budget is why it cleans up so rarely."
							/>
						</span>
						<span class="font-mono">
							{memory.fullCollections} · {gib(memory.gcBudgetBytes)}{memory.serverGc
								? ' (server GC)'
								: ''}
						</span>
					</div>

					{#if processBeforeCollect !== null}
						<p class="text-xs text-muted-foreground">
							Last forced collection: process went from {mib(processBeforeCollect)} to {mib(
								memory.processBytes
							)}. A big drop means the memory was reclaimable, not leaked.
						</p>
					{/if}
				</div>
			{/if}
		</div>

		<div class="flex flex-col gap-3 rounded-md border p-4">
			<div class="flex items-center justify-between">
				<span class="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
					Mistral models
					{#if models?.checkedAt}· checked {new Date(models.checkedAt).toLocaleString()}{/if}
					<InfoTip
						text="Compares the pinned models with Mistral's model list, at startup and then once a day; the refresh button checks right away. Per family: Using is the model the app is configured with (Reference for Large, which the app doesn't use and is set in code), latest is what Mistral's -latest alias points to now. Below that are all versions Mistral lists, with all names of each version: outlined is the one in use, new marks a version that showed up since you last dismissed."
					/>
				</span>

				<div class="flex gap-2">
					{#if hasNewModels}
						<Button
							size="sm"
							variant="outline"
							disabled={modelsBusy}
							onclick={() =>
								modelsAction(() => api.post<ModelsResponse>('/api/dashboard/models/acknowledge'))}
						>
							Dismiss new
						</Button>
					{/if}

					<Button
						size="sm"
						variant="ghost"
						disabled={modelsBusy}
						title="Check now"
						onclick={() =>
							modelsAction(() => api.post<ModelsResponse>('/api/dashboard/models/refresh'))}
					>
						<RefreshCw size={14} />
					</Button>
				</div>
			</div>

			{#if models?.error}
				<span class="text-sm text-destructive">Last check failed: {models.error}</span>
			{/if}

			{#each models?.families ?? [] as family (family.name)}
				<div class="flex flex-col gap-1 border-t pt-2 text-sm">
					<div class="flex items-center justify-between">
						<span class="font-medium">{family.name}</span>
						<span
							class="flex items-center gap-1.5 {NEEDS_ATTENTION.includes(family.status)
								? 'text-orange-600 dark:text-orange-400'
								: 'text-muted-foreground'}"
						>
							{STATUS_LABEL[family.status]}<InfoTip text={STATUS_INFO[family.status]} />
						</span>
					</div>

					<div class="text-muted-foreground">
						{#if family.configured}{family.usedByApp ? 'Using' : 'Reference'}
							<span class="font-mono">{family.configured}</span> ·
						{/if}
						latest <span class="font-mono">{family.latest ?? '—'}</span>
					</div>

					{#if family.deprecation}
						<div class="text-destructive">
							Deprecated on {new Date(
								family.deprecation
							).toLocaleDateString()}{#if family.deprecationReplacement}, replacement: <span
									class="font-mono"
								>
									{family.deprecationReplacement}
								</span>{/if}
						</div>
					{/if}

					<div class="flex flex-wrap gap-1">
						{#each family.models as model (model.names.join(','))}
							<span
								class="rounded border px-1.5 py-0.5 font-mono text-xs
                                {model.isNew
									? 'border-orange-500/50 text-orange-600 dark:text-orange-400'
									: 'text-muted-foreground'}
                                {model.isConfigured ? 'border-foreground/40 text-foreground' : ''}"
							>
								{model.names.join(' / ')}{#if model.isLatest}
									· latest{/if}{#if model.isNew}
									· new{/if}
							</span>
						{/each}
					</div>
				</div>
			{/each}
		</div>
	</div>

	<UsageSection />
</div>
