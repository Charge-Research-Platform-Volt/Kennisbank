<script lang="ts">
	import type { ResourceItem, NavigationTarget } from "$lib/types/resource";
    import { ArrowLeft, ArrowRight, Download, X, Pencil, PencilOff } from "lucide-svelte";
    import { getFileIcon } from "$lib/utils/icons";
    import { untrack, setContext } from "svelte";
	import { formatDate } from "$lib/utils/date";
    import ResourceInspector from "./resource-inspector.svelte";
    import PersonInspector from "./person-inspector.svelte";
    import OrganistationInspector from "./organistation-inspector.svelte";
	import TooltipProvider from "../ui/tooltip/tooltip-provider.svelte";
	import { api } from "$lib/api";

    let {
        item = $bindable<ResourceItem | null>(null),
    }: { item: ResourceItem | null } = $props();

    let history = $state<ResourceItem[]>([]);
    let historyIndex = $state(-1);
    let editMode = $state(false);

    let canGoBack = $derived(historyIndex > 0);
    let canGoForward = $derived(historyIndex < history.length - 1);

    let Icon = $derived(getFileIcon(item?.fileType ?? item?.type ?? ''));

    // Called when item is set from outside (e.g., row click)
    $effect(() => {
        const current = item;
        if (!current) return;

        untrack(() => {
            if (current.id === history[historyIndex]?.id) return;

            history = [...history.slice(0, historyIndex + 1), current];
            historyIndex = history.length - 1;
            editMode = false;
        });
    });

    setContext('navigate', navigate);

    async function navigate(target: NavigationTarget) {
        const endpoint = target.type === 'resource' ? 'resources' : target.type === 'person' ? 'persons' : 'organisations';
        const props = target.type === 'resource' ? 'CreationDate,PublicationDate,PublicationDatePrecision,FileType,Description' : 'CreationDate,Description';

        const result = await api.get<Partial<ResourceItem>>(`/api/${endpoint}/info/${target.id}?properties=${props}`);
        const fullItem: ResourceItem = { fileType: target.type, publicationDate: '', publicationDatePrecision: 'Day', creationDate: '', description: '', ...target, ...result.body };

        history = [...history.slice(0, historyIndex + 1), fullItem];
        historyIndex = history.length - 1;
        item = fullItem;
        editMode = false;
    }

    function goBack() {
        if (!canGoBack) return;

        item = history[--historyIndex];
        editMode = false;
    }

    function goForward() {
        if (!canGoForward) return;

        item = history[++historyIndex];
        editMode = false;
    }

    function close() {
        item = null;
        editMode = false;
    }
</script>

<aside class="bg-background border-l border-border flex flex-col h-full transition-all duration-300 {item ? 'w-110' : 'w-0'} overflow-hidden">
    <TooltipProvider>
        {#if item}
            <!-- Toolbar row -->
            <div class="flex items-center justify-between px-3 py-2 border-b shrink-0">
                <div class="flex items-center gap-1">
                    <!-- Navigation buttons -->
                    <button onclick={goBack} disabled={!canGoBack} class="p-1 cursor-pointer text-muted-foreground hover:text-foreground transition-colors disabled:opacity-30 disabled:cursor-not-allowed">
                        <ArrowLeft size={16} />
                    </button>
                    <button onclick={goForward} disabled={!canGoForward} class="p-1 cursor-pointer text-muted-foreground hover:text-foreground transition-colors disabled:opacity-30 disabled:cursor-not-allowed">
                        <ArrowRight size={16} />
                    </button>
                </div>

                <!-- Download, edit, close buttons -->
                <div class="flex items-center gap-1">
                    {#if item?.type === 'resource'}
                        <button onclick={() => console.log('download')} class="p-1 cursor-pointer text-muted-foreground hover:text-foreground transition-colors">
                            <Download size={16} />
                        </button>
                    {/if}

                    <button onclick={() => { editMode = !editMode}} class="p-1 cursor-pointer text-muted-foreground hover:text-foreground transition-colors">
                        {#if editMode}
                            <PencilOff size={16} />
                        {:else}
                            <Pencil size={16} />
                        {/if}
                    </button>

                    <button onclick={close} class="p-1 cursor-pointer text-muted-foreground hover:text-foreground transition-colors">
                        <X size={16} />
                    </button>
                </div>
            </div>

            <!-- Title row -->
            <div class="flex items-start gap-2 px-3 py-3 border-b">
                <Icon size={16} class="shrink-0 mt-0.75 text-muted-foreground" />
                <span class="text-sm font-medium line-clamp-2" title={item.name}>{item.name}</span>
            </div>

            <!-- Content -->
            <div class="flex-1 overflow-y-auto">
                {#if item.type === 'resource'}
                    <ResourceInspector {item} />
                {:else if item.type === 'person'}
                    <PersonInspector {item} />
                {:else if item.type === 'organisation'}
                    <OrganistationInspector {item} />
                {/if}
            </div>

            <!-- Footer -->
            <div class="flex justify-center gap-4 px-3 py-2 border-t text-xs text-muted-foreground shrink-0">
                {#if item.type === 'resource'}
                    <span>Published: {formatDate(item.publicationDate, item.publicationDatePrecision)}</span>
                {/if}
                <span>Added: {formatDate(item.creationDate) ?? '-'}</span>
            </div>
        {/if}
    </TooltipProvider>
</aside>