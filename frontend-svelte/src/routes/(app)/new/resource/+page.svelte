<script lang="ts">
	import { goto } from '$app/navigation';
	import { api } from '$lib/api';
	import AsyncSelect from '$lib/components/ui/async-select.svelte';
	import InlineSelect from '$lib/components/ui/inline-select.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
    import { fly } from 'svelte/transition';
	import { LanguageCodes } from '$lib/lists/languageCodes';
	import type { DatePrecision, ExtractedMetadata } from '$lib/types/resource';
	import { User, Building2, X, CircleCheck, Sparkles } from 'lucide-svelte';
	import BadgeInput from '$lib/components/ui/badge-input.svelte';
	import { toast } from 'svelte-sonner';
    import ProcessingPhase from './processing-phase.svelte';
    import SelectPhase from './select-phase.svelte';
    import DuplicatePhase from './duplicate-phase.svelte';

	type Phase = 'select' | 'processing' | 'review' | 'duplicate';
    type EntityEntry = {
        extracted: string;
        value: string;
        displayValue: string;
        score?: number | null;
        role?: string;
        authorType?: string;
    };

	let phase = $state<Phase>('review');
	let mode = $state<'file' | 'url'>('file');

	// URL mode
	let url = $state('');

	let jobId = $state<string | null>(null);
	let duplicateId = $state<string | null>(null);
	let fileId = $state<string | null>(null);
	let fileHash = $state<string | null>(null);
	let fileExtension = $state('');

    // Review phase
    let resourceTypeDisplay = $state<string | null>(null);
    let languageDisplay = $state<string | null>(null);

    const searchResourceTypes = async (q: string) => {
        const url = q ? `/api/resources/types/list?search=${encodeURIComponent(q)}` : '/api/resources/types/list';
        const result = await api.get<{ id: string; name: string }[]>(url);
        return result.body.filter((t) => t.name !== 'Unknown');
    };

    const createResourceType = async (name: string) => {
        const result = await api.put<{ id: string; name: string }>('/api/resources/types/new', { name });
        return result.body;
    };

    const searchLanguages = (q: string) =>
        Promise.resolve(
            LanguageCodes.filter((l) => l.label.toLowerCase().includes(q.toLowerCase()))
                .map((l) => ({ id: l.value, name: l.label }))
        );

    const searchPersons = async (q: string) => {
        const result = await api.get<{ id: string; name: string; }[]>(`/api/persons/list?searchQuery=${encodeURIComponent(q)}&pageSize=10&properties=Id,Name`);
        return result.body;
    }

    const searchOrganisations = async (q: string) => {
        const result = await api.get<{ id: string; name: string; }[]>(`/api/organisations/list?searchQuery=${encodeURIComponent(q)}&pageSize=10&properties=Id,Name`);
        return result.body;
    }

    const searchTags = async (q: string) => {
        const url = q ? `/api/tags/tag-page?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}` : '/api/tags/tag-page?pageIndex=1&pageSize=20';
        const r = await api.get<{ tags: { id: string; name: string }[] }>(url);
        return r.body?.tags ?? [];
    }

    const createTag = async (name: string): Promise<{ id: string; name: string } | null> => {
        const response = await fetch('/api/tags/add-user-tag', {
            method: 'PUT',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name }),
        });
        const data = await response.json();
        if (response.ok || response.status === 409) return { id: data.body as string, name };
        return null;
    }

    const searchRegions = async (q: string) => {
        const url = q ? `/api/regions/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name` : '/api/regions/list?pageIndex=1&pageSize=20&properties=Id,Name';
        const r = await api.get<{ id: string; name: string }[]>(url);
        return r.body ?? [];
    }

    const createRegion = async (name: string): Promise<{ id: string; name: string } | null> => {
        const r = await api.put<string>('/api/regions/new', { name });
        return { id: r.body, name };
    }

    let resourceInfo = $state({
        typeId: '',
        title: '',
        languageCode: '',
        publicationDate: '',
        publicationDatePrecision: 'Day' as DatePrecision,
        abstract: '',
        description: '',
        publicationCode: '',
        license: '',
        sourceUrl: '',
        note: '',
    });

    let tags = $state<{ id: string; name: string }[]>([]);
    let regions = $state<{ id: string; name: string }[]>([]);
    let authors = $state<EntityEntry[]>([]);
    let organisations = $state<EntityEntry[]>([]);
    let relatedPersons = $state<EntityEntry[]>([]);
    let dateDay = $state('');
    let dateMonth = $state('');
    let dateYear = $state('');

    function autoresize(node: HTMLTextAreaElement) {
		function resize() {
			node.style.height = 'auto';
			node.style.height = node.scrollHeight + 'px';
		}
		node.addEventListener('input', resize);
		resize();
		return { destroy: () => node.removeEventListener('input', resize) };
	}

    function updateDate() {
        if (dateYear && dateMonth && dateDay) {
            resourceInfo.publicationDatePrecision = 'Day';
            resourceInfo.publicationDate = `${dateYear}-${dateMonth.padStart(2, '0')}-${dateDay.padStart(2, '0')}`;
        } else if (dateYear && dateMonth) {
            resourceInfo.publicationDatePrecision = 'Month';
            resourceInfo.publicationDate = `${dateYear}-${dateMonth.padStart(2, '0')}-01`;
        } else if (dateYear) {
            resourceInfo.publicationDatePrecision = 'Year';
            resourceInfo.publicationDate = `${dateYear}-01-01`;
        } else {
            resourceInfo.publicationDate = '';
        }
    }

    function setDateFromIso(date: string, precision: DatePrecision) {
        if (!date) return;
        const d = new Date(date);
        dateYear = String(d.getFullYear());
        dateMonth = precision !== 'Year' ? String(d.getMonth() + 1) : '';
        dateDay = precision === 'Day' ? String(d.getDate()) : '';
    }

    function getUploadType(ext: string): 'audio' | 'video' | 'document' {
        if (['mp3', 'wav', 'ogg', 'flac', 'm4a', 'aac'].includes(ext)) return 'audio';
        if (['mp4', 'mov', 'avi', 'mkv', 'webm', 'wmv'].includes(ext)) return 'video';
        return 'document';
    }

    let isSubmitting = $state(false);

    async function handleSubmit() {
        isSubmitting = true;

        try {
            const baseDto = {
                Title: resourceInfo.title,
                Description: resourceInfo.description || null,
                TypeId: resourceInfo.typeId,
                LanguageCode: resourceInfo.languageCode,
                PublicationCode: resourceInfo.publicationCode || null,
                PublicationDate: resourceInfo.publicationDate ? new Date(resourceInfo.publicationDate).toISOString() : null,
                PublicationDatePrecision: resourceInfo.publicationDate ? resourceInfo.publicationDatePrecision : null,
                License: resourceInfo.license || null,
                SourceUrl: resourceInfo.sourceUrl || null,
                Note: resourceInfo.note || null,
                Tags: tags.map(t => t.id),
                Authors: authors.map(a => ({ value: a.value, type: a.authorType?.toLowerCase() ?? 'person' })),
                Organisations: organisations.map(o => ({ Id: o.value, Relation: o.role || null })),
                RelatedPersons: relatedPersons.map(p => ({ Id: p.value, Relation: p.role || null })),
                Regions: regions.map(r => r.id),
            };

            let result: { body: string };

            if (mode === 'url') {
                result = await api.put<string>('/api/resources/new', { uploadType: 'website', url, ...baseDto });
            } else {
                const uploadType = getUploadType(fileExtension);
                result = await api.put<string>('/api/resources/new', {
                    uploadType,
                    ...baseDto,
                    Id: fileId,
                    Hash: fileHash,
                    FileExtension: fileExtension,
                    ...(uploadType === 'document' ? { Abstract: resourceInfo.abstract } : {}),
                });
            }

            goto(`/library?inspectorId=${result.body}&inspectorType=resource`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Something went wrong');
        } finally {
            isSubmitting = false;
        }
    }

    function onSelectComplete(result: { mode: 'file' | 'url'; url: string; jobId: string; fileId: string | null; fileHash: string | null; fileExtension: string }) {
        mode = result.mode;
        url = result.url;
        jobId = result.jobId;
        fileId = result.fileId;
        fileHash = result.fileHash;
        fileExtension = result.fileExtension;
        phase = 'processing';
    }

    function onDuplicate(id: string) {
        duplicateId = id;
        phase = 'duplicate';
    }

    async function onProcessingComplete(metadata: ExtractedMetadata | null) {
        if (metadata) {
            if (mode === 'url') resourceInfo.sourceUrl = url;
            resourceInfo.title = metadata.title ?? '';
            resourceInfo.languageCode = metadata.languageCode ?? '';
            languageDisplay = LanguageCodes.find((l) => l.value === resourceInfo.languageCode)?.label ?? null;
            resourceInfo.publicationDate = metadata.publicationDate ?? '';
            resourceInfo.publicationDatePrecision =
                metadata.publicationDatePrecision === 'Exact' ? 'Day'
                : metadata.publicationDatePrecision === 'YearMonth' ? 'Month'
                : 'Year';
            setDateFromIso(resourceInfo.publicationDate, resourceInfo.publicationDatePrecision);
            resourceInfo.abstract = metadata.abstract ?? '';
            resourceInfo.description = metadata.description ?? '';
            resourceInfo.publicationCode = metadata.publicationCode ?? '';

            tags = await Promise.all(metadata.tags.map(async (name: string) => {
                const results = await searchTags(name);
                const exact = results.find(r => r.name.toLowerCase() === name.toLowerCase());
                return exact ?? { id: name, name };
            }));

            const toEntry = (e: { name: string; type: string; similars: { id: string; name: string; score: number }[] }): EntityEntry => {
                const top = e.similars[0];
                return top && top.score >= 0.8
                    ? { extracted: e.name, value: top.id, displayValue: top.name, authorType: e.type, score: top.score }
                    : { extracted: e.name, value: e.name, displayValue: e.name, authorType: e.type, score: top?.score ?? null };
            };

            authors = metadata.authors.map(toEntry);
            organisations = metadata.organisations.map(toEntry);
            relatedPersons = metadata.relatedPersons.map(toEntry);
        }

        phase = 'review';
    }

    function onProcessingSkip() {
        phase = 'review';
    }


</script>

{#if phase === 'review'}
    <div class="flex flex-col h-full p-6 gap-4">
        <!-- Header -->
        <div class="flex items-center justify-between border-b border-border pb-4">
            <h1 class="text-2xl font-semibold">Review Resource</h1>
<Button onclick={handleSubmit} disabled={isSubmitting} class="cursor-pointer">
                {isSubmitting ? 'Saving...' : '+ Add Resource'}
            </Button>
        </div>

        <!-- Grid -->
        <div class="flex-1 min-h-0 grid grid-cols-[3fr_2fr] divide-x divide-border">
            <!-- Resource information -->
            <div class="flex flex-col gap-6 overflow-y-auto px-6 py-4 pb-10">

                <!-- Title -->
                <div class="flex flex-col gap-0.5 border-b border-transparent focus-within:border-border transition-colors pb-1">
                    {#if resourceInfo.title}
                        <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Title</span>
                    {/if}
                    <input
                        bind:value={resourceInfo.title}
                        placeholder="Title"
                        class="text-xl font-semibold bg-transparent outline-none w-full placeholder:text-muted-foreground/50"
                    />
                </div>

                <!-- Type + Language -->
                <div class="flex gap-4">
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors">
                        <span class="text-xs text-muted-foreground">Type</span>
                        <AsyncSelect
                            bind:value={resourceInfo.typeId}
                            bind:displayValue={resourceTypeDisplay}
                            search={searchResourceTypes}
                            oncreate={createResourceType}
                            placeholder="Select or create type..."
                            variant="ghost"
                        />
                    </div>
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors">
                        <span class="text-xs text-muted-foreground">Language</span>
                        <AsyncSelect
                            bind:value={resourceInfo.languageCode}
                            bind:displayValue={languageDisplay}
                            search={searchLanguages}
                            placeholder="Select language..."
                            variant="ghost"
                        />
                    </div>
                </div>

                <!-- Abstract -->
                {#if mode === 'file'}
                    <div class="flex flex-col gap-0.5 border-b border-transparent focus-within:border-border transition-colors pb-1">
                        {#if resourceInfo.abstract}
                            <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Abstract</span>
                        {/if}
                        <textarea
                            bind:value={resourceInfo.abstract}
                            placeholder="Abstract"
                            rows={1}
                            use:autoresize
                            class="bg-transparent outline-none w-full resize-none overflow-hidden text-sm placeholder:text-muted-foreground/50"
                        ></textarea>
                    </div>
                {/if}

                <!-- Description -->
                <div class="flex flex-col gap-0.5 border-b border-transparent focus-within:border-border transition-colors pb-1">
                    {#if resourceInfo.description}
                        <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Description</span>
                    {/if}
                    <textarea
                        bind:value={resourceInfo.description}
                        placeholder="Description"
                        rows={1}
                        use:autoresize
                        class="bg-transparent outline-none w-full resize-none overflow-hidden text-sm placeholder:text-muted-foreground/50"
                    ></textarea>
                </div>

                <!-- Tags -->
                <div class="flex flex-col gap-2">
                    <span class="text-xs text-muted-foreground">Tags</span>
                    <BadgeInput bind:items={tags} search={searchTags} oncreate={createTag} placeholder="Search or create tag..." />
                </div>

                <!-- Regions -->
                <div class="flex flex-col gap-2">
                    <span class="text-xs text-muted-foreground">Regions</span>
                    <BadgeInput bind:items={regions} search={searchRegions} oncreate={createRegion} placeholder="Search or create region..." />
                </div>

                <!-- Publication Date + Publication Code -->
                <div class="flex gap-4 items-end">
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors pb-1">
                        <span class="text-xs text-muted-foreground">Publication Date</span>
                        <div class="flex items-center gap-1 text-sm">
                            <input type="number" bind:value={dateDay} onblur={updateDate} min="1" max="31" placeholder="DD" class="bg-transparent outline-none w-8 placeholder:text-muted-foreground/50" />
                            <span class="text-muted-foreground/30">/</span>
                            <input type="number" bind:value={dateMonth} onblur={updateDate} min="1" max="12" placeholder="MM" class="bg-transparent outline-none w-8 placeholder:text-muted-foreground/50" />
                            <span class="text-muted-foreground/30">/</span>
                            <input type="number" bind:value={dateYear} onblur={updateDate} min="1000" max="2100" placeholder="YYYY" class="bg-transparent outline-none w-14 placeholder:text-muted-foreground/50" />
                        </div>
                    </div>
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors pb-1">
                        {#if resourceInfo.publicationCode}
                            <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Publication Code</span>
                        {/if}
                        <input
                            bind:value={resourceInfo.publicationCode}
                            placeholder="Publication Code"
                            class="bg-transparent outline-none w-full text-sm placeholder:text-muted-foreground/50"
                        />
                    </div>
                </div>

                <!-- License + Source URL -->
                <div class="flex gap-4">
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors pb-1">
                        {#if resourceInfo.license}
                            <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">License</span>
                        {/if}
                        <input
                            bind:value={resourceInfo.license}
                            placeholder="License"
                            class="bg-transparent outline-none w-full text-sm placeholder:text-muted-foreground/50"
                        />
                    </div>
                    <div class="flex flex-col gap-0.5 flex-1 min-w-0 border-b border-transparent focus-within:border-border transition-colors pb-1">
                        {#if resourceInfo.sourceUrl}
                            <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Source URL</span>
                        {/if}
                        <input
                            type="url"
                            bind:value={resourceInfo.sourceUrl}
                            placeholder="Source URL"
                            class="bg-transparent outline-none w-full text-sm placeholder:text-muted-foreground/50"
                        />
                    </div>
                </div>

                <!-- Note -->
                <div class="flex flex-col gap-0.5 border-b border-transparent focus-within:border-border transition-colors pb-1">
                    {#if resourceInfo.note}
                        <span transition:fly={{ y: 4, duration: 150 }} class="text-xs text-muted-foreground">Note</span>
                    {/if}
                    <textarea
                        bind:value={resourceInfo.note}
                        placeholder="Notes"
                        rows={1}
                        use:autoresize
                        class="bg-transparent outline-none w-full resize-none overflow-hidden text-sm placeholder:text-muted-foreground/50"
                    ></textarea>
                </div>

            </div>

            <!-- Connections -->
            <div class="flex flex-col gap-4 overflow-y-auto px-4 py-2 pb-10">

                <!-- Authors -->
                <div class="flex flex-col gap-2">
                    <div>
                        <h3 class="text-xs font-medium text-foreground uppercase tracking-wide">Authors</h3>
                        <p class="text-xs text-muted-foreground">Wrote or contributed to this resource</p>
                    </div>
                    {#each authors as entry, i (i)}
                        {#if i > 0}<div class="border-t border-border/50"></div>{/if}
                        <div class="flex flex-col gap-1">
                            {#if entry.extracted}
                                <div class="flex items-center gap-1 text-xs text-muted-foreground">
                                    <span class="shrink-0">Found:</span>
                                    <span class="font-mono truncate" title={entry.extracted}>"{entry.extracted}"</span>
                                    {#if entry.score != null && entry.score >= 0.8}
                                        <span title="Matched to existing entity with {Math.min(100, Math.round(entry.score * 100))}% confidence" class="flex items-center gap-0.5 text-emerald-500 font-medium ml-auto shrink-0">
                                            <CircleCheck size={11} />{Math.min(100, Math.round(entry.score * 100))}%
                                        </span>
                                    {:else}
                                        <span title="No confident match found — will be created as a new entity" class="flex items-center gap-0.5 text-amber-400 font-medium ml-auto shrink-0">
                                            <Sparkles size={11} />New
                                        </span>
                                    {/if}
                                </div>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <button
                                    onclick={() => { entry.authorType = entry.authorType === 'Organisation' ? 'Person' : 'Organisation'; entry.value = ''; entry.displayValue = ''; }}
                                    class="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground shrink-0 cursor-pointer border border-border/50 rounded px-1.5 py-0.5 hover:border-border transition-colors">
                                    {#if entry.authorType === 'Organisation'}
                                        <Building2 size={13} />Org
                                    {:else}
                                        <User size={13} />Person
                                    {/if}
                                </button>
                                <div class="flex-1 min-w-0">
                                    <InlineSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={entry.authorType === 'Organisation' ? searchOrganisations : searchPersons}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if !entry.extracted && entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span title="Will be created as a new entity" class="flex items-center gap-0.5 text-xs text-amber-400 font-medium shrink-0">
                                        <Sparkles size={11} />New
                                    </span>
                                {/if}
                                <button onclick={() => authors = authors.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                        </div>
                    {/each}
                    <button onclick={() => authors = [...authors, { extracted: '', value: '', displayValue: '', authorType: 'Person' }]} class="w-full border border-dashed border-border rounded text-xs text-muted-foreground hover:text-foreground hover:border-foreground/40 cursor-pointer py-1.5 transition-colors">+ Add</button>
                </div>

                <div class="border-t border-border"></div>

                <!-- Related Persons -->
                <div class="flex flex-col gap-2">
                    <div>
                        <h3 class="text-xs font-medium text-foreground uppercase tracking-wide">People</h3>
                        <p class="text-xs text-muted-foreground">Mentioned or otherwise connected</p>
                    </div>
                    {#each relatedPersons as entry, i (i)}
                        {#if i > 0}<div class="border-t border-border/50"></div>{/if}
                        <div class="flex flex-col gap-1">
                            {#if entry.extracted}
                                <div class="flex items-center gap-1 text-xs text-muted-foreground">
                                    <span class="shrink-0">Found:</span>
                                    <span class="font-mono truncate" title={entry.extracted}>"{entry.extracted}"</span>
                                    {#if entry.score != null && entry.score >= 0.8}
                                        <span title="Matched to existing entity with {Math.min(100, Math.round(entry.score * 100))}% confidence" class="flex items-center gap-0.5 text-emerald-500 font-medium ml-auto shrink-0">
                                            <CircleCheck size={11} />{Math.min(100, Math.round(entry.score * 100))}%
                                        </span>
                                    {:else}
                                        <span title="No confident match found — will be created as a new entity" class="flex items-center gap-0.5 text-amber-400 font-medium ml-auto shrink-0">
                                            <Sparkles size={11} />New
                                        </span>
                                    {/if}
                                </div>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <div class="flex-1 min-w-0">
                                    <InlineSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={searchPersons}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if !entry.extracted && entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span title="Will be created as a new entity" class="flex items-center gap-0.5 text-xs text-amber-400 font-medium shrink-0">
                                        <Sparkles size={11} />New
                                    </span>
                                {/if}
                                <button onclick={() => relatedPersons = relatedPersons.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                            <div class="border-b border-transparent focus-within:border-border transition-colors pb-0.5">
                                <input bind:value={entry.role} placeholder="Role..." class="bg-transparent outline-none w-full text-xs placeholder:text-muted-foreground/50" />
                            </div>
                        </div>
                    {/each}
                    <button onclick={() => relatedPersons = [...relatedPersons, { extracted: '', value: '', displayValue: '' }]} class="w-full border border-dashed border-border rounded text-xs text-muted-foreground hover:text-foreground hover:border-foreground/40 cursor-pointer py-1.5 transition-colors">+ Add</button>
                </div>

                <div class="border-t border-border"></div>

                <!-- Related Organisations -->
                <div class="flex flex-col gap-2">
                    <div>
                        <h3 class="text-xs font-medium text-foreground uppercase tracking-wide">Organisations</h3>
                        <p class="text-xs text-muted-foreground">Mentioned or otherwise connected</p>
                    </div>
                    {#each organisations as entry, i (i)}
                        {#if i > 0}<div class="border-t border-border/50"></div>{/if}
                        <div class="flex flex-col gap-1">
                            {#if entry.extracted}
                                <div class="flex items-center gap-1 text-xs text-muted-foreground">
                                    <span class="shrink-0">Found:</span>
                                    <span class="font-mono truncate" title={entry.extracted}>"{entry.extracted}"</span>
                                    {#if entry.score != null && entry.score >= 0.8}
                                        <span title="Matched to existing entity with {Math.min(100, Math.round(entry.score * 100))}% confidence" class="flex items-center gap-0.5 text-emerald-500 font-medium ml-auto shrink-0">
                                            <CircleCheck size={11} />{Math.min(100, Math.round(entry.score * 100))}%
                                        </span>
                                    {:else}
                                        <span title="No confident match found — will be created as a new entity" class="flex items-center gap-0.5 text-amber-400 font-medium ml-auto shrink-0">
                                            <Sparkles size={11} />New
                                        </span>
                                    {/if}
                                </div>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <div class="flex-1 min-w-0">
                                    <InlineSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={searchOrganisations}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if !entry.extracted && entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span title="Will be created as a new entity" class="flex items-center gap-0.5 text-xs text-amber-400 font-medium shrink-0">
                                        <Sparkles size={11} />New
                                    </span>
                                {/if}
                                <button onclick={() => organisations = organisations.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                            <div class="border-b border-transparent focus-within:border-border transition-colors pb-0.5">
                                <input bind:value={entry.role} placeholder="Role..." class="bg-transparent outline-none w-full text-xs placeholder:text-muted-foreground/50" />
                            </div>
                        </div>
                    {/each}
                    <button onclick={() => organisations = [...organisations, { extracted: '', value: '', displayValue: '' }]} class="w-full border border-dashed border-border rounded text-xs text-muted-foreground hover:text-foreground hover:border-foreground/40 cursor-pointer py-1.5 transition-colors">+ Add</button>
                </div>
            </div>
        </div>
    </div>
{:else}
    <div class="flex h-full items-start justify-center p-8 pt-24">
        <div class="flex w-full max-w-xl flex-col gap-6">
            <!-- Header -->
            <div>
                {#if phase === 'processing'}
                    <h1 class="text-2xl font-semibold text-center">Analyzing Resource...</h1>
                {:else if phase === 'duplicate'}
                    <h1 class="text-2xl font-semibold">Duplicate Found</h1>
                {:else}
                    <h1 class="text-2xl font-semibold">Add Resource</h1>
                {/if}
            </div>

            {#if phase === 'select'}
                <SelectPhase oncomplete={onSelectComplete} onduplicate={onDuplicate} />
            {:else if phase === 'processing'}
                <ProcessingPhase jobId={jobId!} oncomplete={onProcessingComplete} onskip={onProcessingSkip} />
            {:else if phase === 'duplicate'}
                <DuplicatePhase duplicateId={duplicateId!} onback={() => phase = 'select'} />
            {/if}
        </div>
    </div>
{/if}

<style>
    input[type=number]::-webkit-inner-spin-button,
    input[type=number]::-webkit-outer-spin-button {
        -webkit-appearance: none;
        appearance: none;
        margin: 0;
    }
    input[type=number] {
        -moz-appearance: textfield;
        appearance: textfield;
    }
</style>