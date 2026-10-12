<script lang="ts">
    import { api } from "$lib/api";
    import { onMount } from "svelte";
    import Spinner from "$lib/components/ui/spinner/spinner.svelte";
    import { CircleCheck, TriangleAlert } from '@lucide/svelte';

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

    let live = $state<LiveStatus | null>(null);

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

    <div class="flex gap-4 text-sm">
        <a href="/dev/new-changelog" class="text-muted-foreground underline hover:text-foreground">New changelog entry</a>
        <a href="/dev/reembed" class="text-muted-foreground underline hover:text-foreground">Embedding status</a>
    </div>
</div>