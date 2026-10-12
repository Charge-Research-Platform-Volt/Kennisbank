<script lang="ts">
    import { api } from "$lib/api";
    import { onMount } from "svelte";
    import Spinner from "$lib/components/ui/spinner/spinner.svelte";
    import { CircleCheck, RefreshCw, TriangleAlert } from '@lucide/svelte';
    import { Button } from '$lib/components/ui/button';

    type LiveStatus = {
        connectedUsers: number;
        activeUsers: number;
        activeWindowMinutes: number;
        activeChatStreams: number;
        queue: { pending: number; running: number; };
        reembedRunning: boolean;
        extractionJobs: number;
        safeToDeploy: boolean;
    };

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

    type ModelsResponse = { checkedAt: string | null; error: string | null; families: ModelFamilyStatus[] };

    const STATUS_LABEL: Record<ModelFamilyStatus['status'], string> = {
        UpToDate: 'Up to date',
        UpdateAvailable: 'Update available',
        NotPinned: 'Not pinned (-latest)',
        Missing: 'Pinned model no longer listed',
        WatchOnly: 'Watching',
        Unknown: 'Unknown'
    };

    const NEEDS_ATTENTION: ModelFamilyStatus['status'][] = ['UpdateAvailable', 'Missing', 'NotPinned'];

    let live = $state<LiveStatus | null>(null);
    let models = $state<ModelsResponse | null>(null);
    let modelsBusy = $state(false);
    let hasNewModels = $derived(models?.families.some((f) => f.models.some((m) => m.isNew)) ?? false);

    async function modelsAction(request: () => Promise<ModelsResponse>) {
        modelsBusy = true;
        try {
            models = await request();
        }
        finally {
            modelsBusy = false;
        }
    }

    async function fetchLive() {
        try {
            live = await api.get<LiveStatus>('/api/dashboard/live');
        }
        catch {
            // Keep showing last known values
        }
    }

    onMount(() => {
        fetchLive();
        modelsAction(() => api.get<ModelsResponse>('/api/dashboard/models'));
        const interval = setInterval(fetchLive, 10_000);
        return () => clearInterval(interval);
    });
</script>

<div class="mx-auto flex h-full max-w-4xl flex-col gap-6 overflow-y-auto p-8">
    <h1 class="text-lg font-semibold">Dashboard</h1>

    {#if !live}
        <div class="flex h-full items-center justify-center">
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
        </div>

        <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-2 rounded-md border p-4">
                <span class="text-sm font-medium text-muted-foreground">Users (excluding you)</span>

                <div class="flex items-center justify-between text-sm">
                    <span>Online</span><span class="font-mono">{live.connectedUsers}</span>
                </div>
                
                <div class="flex items-center justify-between text-sm">
                    <span>Active in last {live.activeWindowMinutes} min</span>
                    <span class="font-mono">{live.activeUsers}</span>
                </div>
            </div>

            <div class="flex flex-col gap-2 rounded-md border p-4">
                <span class="text-sm font-medium text-muted-foreground">Running work</span>

                <div class="flex items-center justify-between text-sm">
                    <span>Chat responses</span><span class="font-mono">{live.activeChatStreams}</span>
                </div>

                <div class="flex items-center justify-between text-sm">
                    <span>Queue (running / pending)</span>
                    <span class="font-mono">{live.queue.running} / {live.queue.pending}</span>
                </div>

                <div class="flex items-center justify-between text-sm">
                    <span>Metadata extraction jobs</span><span class="font-mono">{live.extractionJobs}</span>
                </div>

                <div class="flex items-center justify-between text-sm">
                    <span>Re-embed run</span><span class="font-mono">{live.reembedRunning ? 'running' : 'idle'}</span>
                </div>
            </div>
        </div>
    {/if}

    <div class="flex flex-col gap-3 rounded-md border p-4">
        <div class="flex items-center justify-between">
            <span class="text-sm font-medium text-muted-foreground">
                Mistral models
                {#if models?.checkedAt}· checked {new Date(models.checkedAt).toLocaleString()}{/if}
            </span>

            <div class="flex gap-2">
                {#if hasNewModels}
                    <Button
                        size="sm"
                        variant="outline"
                        disabled={modelsBusy}
                        onclick={() => modelsAction(() => api.post<ModelsResponse>('/api/dashboard/models/acknowledge'))}
                    >
                        Dismiss new
                    </Button>
                {/if}

                <Button
                    size="sm"
                    variant="ghost"
                    disabled={modelsBusy}
                    title="Check now"
                    onclick={() => modelsAction(() => api.post<ModelsResponse>('/api/dashboard/models/refresh'))}
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
                    <span class={NEEDS_ATTENTION.includes(family.status) ? 'text-orange-600 dark:text-orange-400' : 'text-muted-foreground'}>
                        {STATUS_LABEL[family.status]}
                    </span>
                </div>

                <div class="text-muted-foreground">
                    {#if family.configured}{family.usedByApp ? 'Using' : 'Reference'} <span class="font-mono">{family.configured}</span> · {/if}
                    latest <span class="font-mono">{family.latest ?? '—'}</span>
                </div>

                {#if family.deprecation}
                    <div class="text-destructive">
                        Deprecated on {new Date(family.deprecation).toLocaleDateString()}{#if family.deprecationReplacement}, replacement: <span class="font-mono">{family.deprecationReplacement}</span>{/if}
                    </div>
                {/if}

                <div class="flex flex-wrap gap-1">
                    {#each family.models as model (model.names.join(','))}
                        <span
                            class="rounded border px-1.5 py-0.5 font-mono text-xs
                                {model.isNew ? 'border-orange-500/50 text-orange-600 dark:text-orange-400' : 'text-muted-foreground'}
                                {model.isConfigured ? 'border-foreground/40 text-foreground' : ''}"
                        >
                            {model.names.join(' / ')}{#if model.isLatest} · latest{/if}{#if model.isNew} · new{/if}
                        </span>
                    {/each}
                </div>
            </div>
        {/each}
    </div>

    <div class="flex gap-4 text-sm">
        <a href="/dev/new-changelog" class="text-muted-foreground underline hover:text-foreground">New changelog entry</a>
        <a href="/dev/reembed" class="text-muted-foreground underline hover:text-foreground">Embedding status</a>
    </div>
</div>