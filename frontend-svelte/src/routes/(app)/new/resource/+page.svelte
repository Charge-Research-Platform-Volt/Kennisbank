<script lang="ts">
	import { goto } from '$app/navigation';
	import { api } from '$lib/api';
	import AsyncSelect from '$lib/components/ui/async-select.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import Input from '$lib/components/ui/input/input.svelte';
	import Label from '$lib/components/ui/label/label.svelte';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { Textarea } from '$lib/components/ui/textarea';
	import { LanguageCodes } from '$lib/lists/languageCodes';
	import type { DatePrecision, ExtractedMetadata } from '$lib/types/resource';
	import { hashFile, uploadFile } from '$lib/upload';
	import { Upload, FileText, User, Building2, X } from 'lucide-svelte';

	type Phase = 'select' | 'processing' | 'review' | 'duplicate';
	type Mode = 'file' | 'url';
    type EntityEntry = {
        extracted: string;
        value: string;
        displayValue: string;
        role?: string;
        authorType?: string;
    };

	let phase = $state<Phase>('select');
	let mode = $state<Mode>('file');

	// File mode
	let selectedFile = $state<File | null>(null);
	let isDragging = $state(false);
	let supportedExtensions = $state<string[]>([]);

	let fileInput = $state<HTMLInputElement | null>(null);
	let fileError = $state<string | null>(null);

	// URL mode
	let url = $state('');

	const canProceed = $derived(mode === 'file' ? selectedFile !== null : url.trim().length > 0);

	// Upload
	let uploadStep = $state<'hashing' | 'uploading' | 'starting' | null>(null);
	let isStarting = $state(false);
	let startError = $state<string | null>(null);
	let jobId = $state<string | null>(null);
	let duplicateId = $state<string | null>(null);
	let fileId = $state<string | null>(null);
	let fileHash = $state<string | null>(null);

	// Processing phase
    let statusMessage = $state('');
    let progressPercentage = $state(0);
    let processingError = $state<string | null>(null);
    let extractedMetadata = $state<ExtractedMetadata | null>(null);

    // Review phase
    let submitError = $state<string | null>(null);
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

    let tags = $state<string[]>([]);
    let authors = $state<EntityEntry[]>([]);
    let organisations = $state<EntityEntry[]>([]);
    let relatedPersons = $state<EntityEntry[]>([]);
    let dateInputStr = $state('');

    function getDateInputStr(date: string, precision: DatePrecision): string {
        if (!date) return '';
        const d = new Date(date);
        if (precision === 'Year') return String(d.getFullYear());
        if (precision === 'Month') return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
        return date.split('T')[0];
    }

    function toIsoDate(val: string, prec: DatePrecision): string {
        if (!val) return '';
        if (prec === 'Year') return `${val}-01-01`;
        if (prec === 'Month') return `${val}-01`;
        return val;
    }

    function setDatePrecision(p: DatePrecision) {
        resourceInfo.publicationDatePrecision = p;
        dateInputStr = getDateInputStr(resourceInfo.publicationDate, p);
    }

    function getUploadType(ext: string): 'audio' | 'video' | 'document' {
        if (['mp3', 'wav', 'ogg', 'flac', 'm4a', 'aac'].includes(ext)) return 'audio';
        if (['mp4', 'mov', 'avi', 'mkv', 'webm', 'wmv'].includes(ext)) return 'video';
        return 'document';
    }

    let isSubmitting = $state(false);

    async function handleSubmit() {
        submitError = null;
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
                Tags: tags,
                Authors: authors.map(a => ({ value: a.value, type: a.authorType?.toLowerCase() ?? 'person' })),
                Organisations: organisations.map(o => ({ Id: o.value, Relation: o.role || null })),
                RelatedPersons: relatedPersons.map(p => ({ Id: p.value, Relation: p.role || null })),
                Regions: [],
            };

            let result: { body: string };

            if (mode === 'url') {
                result = await api.put<string>('/api/resources/new', { uploadType: 'website', ...baseDto });
            } else {
                const ext = selectedFile!.name.split('.').pop()?.toLowerCase() ?? '';
                const uploadType = getUploadType(ext);
                result = await api.put<string>('/api/resources/new', {
                    uploadType,
                    ...baseDto,
                    Id: fileId,
                    Hash: fileHash,
                    FileExtension: ext,
                    ...(uploadType === 'document' ? { Abstract: resourceInfo.abstract } : {}),
                });
            }

            goto(`/library?inspectorId=${result.body}&inspectorType=resource`);
        } catch (e) {
            submitError = e instanceof Error ? e.message : 'Something went wrong';
        } finally {
            isSubmitting = false;
        }
    }

	function selectFile(file: File | undefined) {
		fileError = null;

		if (!file) return;

		const ext = file.name.split('.').pop()?.toLowerCase() ?? '';

		if (supportedExtensions.length > 0 && !supportedExtensions.includes(ext)) {
			fileError = 'This file is not supported.';
			return;
		}

		selectedFile = file;
	}

	async function handleNext() {
		isStarting = true;
		startError = null;

		try {
			let extractType: 'file' | 'web';
			let extractValue: string;

			if (mode === 'file') {
				uploadStep = 'hashing';
				const hash = await hashFile(selectedFile!);
				const dup = await api.get<{ exists: boolean; id: string }>(
					`/api/resources/exists?hash=${encodeURIComponent(hash)}`
				);

				if (dup.body.exists) {
					duplicateId = dup.body.id;
					phase = 'duplicate';
					return;
				}

				uploadStep = 'uploading';
				const objectName = await uploadFile(selectedFile!);
				fileId = objectName;
				fileHash = hash;
				extractValue = objectName;
				extractType = 'file';
			} else {
				const dup = await api.get<{ exists: boolean; id: string }>(
					`/api/resources/exists?url=${encodeURIComponent(url)}`
				);

				if (dup.body.exists) {
					duplicateId = dup.body.id;
					phase = 'duplicate';
					return;
				}

				extractType = 'web';
				extractValue = url;
			}

			uploadStep = 'starting';
			const result = await api.post<{ jobId: string }>(
				`/api/ai/extract-metadata/start?type=${extractType}&value=${encodeURIComponent(extractValue)}`
			);

			jobId = result.body.jobId;
			phase = 'processing';
		} catch (e) {
			startError = e instanceof Error ? e.message : 'Something went wrong';
		} finally {
			isStarting = false;
			uploadStep = null;
		}
	}

    $effect(() => {
        if (phase !== 'processing' || !jobId) return;

        const interval = setInterval(async () => {
            try {
                const res = await api.get<{ status: string; statusMessage: string; progressPercentage: number; result: ExtractedMetadata | null; errorMessage: string | null; }>(`/api/ai/extract-metadata/status/${jobId}`);

                statusMessage = res.body.statusMessage;
                progressPercentage = res.body.progressPercentage;

                if (res.body.status === 'Completed') {
                    extractedMetadata = res.body.result;
                    
                    // Fill in extracted metadata
                    if (extractedMetadata) {
                        resourceInfo.title = extractedMetadata.title ?? '';
                        resourceInfo.languageCode = extractedMetadata.languageCode ?? '';
                        languageDisplay = LanguageCodes.find((l) => l.value === resourceInfo.languageCode)?.label ?? null;
                        resourceInfo.publicationDate = extractedMetadata.publicationDate ?? '';
                        resourceInfo.publicationDatePrecision =
                            extractedMetadata.publicationDatePrecision === 'Exact' ? 'Day'
                            : extractedMetadata.publicationDatePrecision === 'YearMonth' ? 'Month'
                            : 'Year';
                        dateInputStr = resourceInfo.publicationDatePrecision
                            ? getDateInputStr(resourceInfo.publicationDate, resourceInfo.publicationDatePrecision)
                            : '';
                        resourceInfo.abstract = extractedMetadata.abstract ?? '';
                        resourceInfo.description = extractedMetadata.description ?? '';
                        resourceInfo.publicationCode = extractedMetadata.publicationCode ?? '';

                        tags = [...extractedMetadata.tags];

                        const toEntry = (e: { name: string; type: string; similars: { id: string; name: string; score: number }[] }): EntityEntry => {
                            const top = e.similars[0];
                            return top && top.score >= 0.8
                                ? { extracted: e.name, value: top.id, displayValue: top.name, authorType: e.type }
                                : { extracted: e.name, value: e.name, displayValue: e.name, authorType: e.type };
                        };

                        authors = extractedMetadata.authors.map(toEntry);
                        organisations = extractedMetadata.organisations.map(toEntry);
                        relatedPersons = extractedMetadata.relatedPersons.map(toEntry);
                    }

                    phase = 'review';
                } else if (res.body.status === 'Failed') {
                    processingError = res.body.errorMessage ?? 'Processing Failed'
                }
            } catch { /* Ignore errors */ }
        }, 2000);

        return () => clearInterval(interval);
    });

	api.get<{ document: string[] }>('/api/resources/supported_extensions')
		.then((r) => (supportedExtensions = r.body.document))
		.catch(() => {});

</script>

{#if phase === 'review'}
    <div class="flex flex-col h-full p-6 gap-4">
        <!-- Header -->
        <div class="flex items-center justify-between border-b border-border pb-4">
            <h1 class="text-2xl font-semibold">Review Resource</h1>
            {#if submitError}
                <p class="text-sm text-destructive">{submitError}</p>
            {/if}
            <Button onclick={handleSubmit} disabled={isSubmitting} class="cursor-pointer">
                {isSubmitting ? 'Saving...' : '+ Add Resource'}
            </Button>
        </div>

        <!-- Grid -->
        <div class="flex-1 min-h-0 grid grid-cols-2 divide-x divide-border">
            <!-- Resource information -->
            <div class="flex flex-col gap-4 overflow-y-auto px-4 py-2">
                <!-- Heading -->
                <h2 class="text-sm font-semibold">Resource Information</h2>

                <!-- Type -->
                <div class="flex flex-col gap-1.5">
                    <Label>Type</Label>
                    <AsyncSelect
                        bind:value={resourceInfo.typeId}
                        bind:displayValue={resourceTypeDisplay}
                        search={searchResourceTypes}
                        oncreate={createResourceType}
                        placeholder="Select type..."
                    />
                </div>

                <!-- Title -->
                <div class="flex flex-col gap-1.5">
                    <Label>Title</Label>
                    <Input bind:value={resourceInfo.title} />
                </div>

                <!-- Language -->
                <div class="flex flex-col gap-1.5">
                    <Label>Language</Label>
                    <AsyncSelect
                        bind:value={resourceInfo.languageCode}
                        bind:displayValue={languageDisplay}
                        search={searchLanguages}
                        placeholder="Select language..."
                    />
                </div>

                <!-- Publication Date -->
                <div class="flex flex-col gap-1.5">
                    <div class="flex justify-between items-center">
                        <Label>Publication Date</Label>
                        <div class="flex gap-2 items-center">
                            <div class="flex rounded-md border border-input text-xs overflow-hidden">
                                <button type="button" onclick={() => setDatePrecision('Year')} class="px-2 py-1 {resourceInfo.publicationDatePrecision === 'Year' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'}">Year</button>
                                <button type="button" onclick={() => setDatePrecision('Month')} class="px-2 py-1 border-x border-input {resourceInfo.publicationDatePrecision === 'Month' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'}">Month</button>
                                <button type="button" onclick={() => setDatePrecision('Day')} class="px-2 py-1 {resourceInfo.publicationDatePrecision === 'Day' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'}">Day</button>
                            </div>
                        </div>
                    </div>

                    {#if resourceInfo.publicationDatePrecision === 'Year'}
                        <input type="number" bind:value={dateInputStr} onblur={() => resourceInfo.publicationDate = toIsoDate(dateInputStr, 'Year')} min="1900" max="2100" placeholder="YYYY" class="h-9 w-24 rounded-md border border-input bg-transparent px-3 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring" />
                    {:else if resourceInfo.publicationDatePrecision === 'Month'}
                        <div class="flex gap-2">
                            <select
                                value={dateInputStr.split('-')[1] ?? ''}
                                onchange={(e) => { const yr = dateInputStr.split('-')[0] || String(new Date().getFullYear()); dateInputStr = `${yr}-${(e.target as HTMLSelectElement).value}`; resourceInfo.publicationDate = toIsoDate(dateInputStr, 'Month'); }}
                                class="h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
                            >
                                <option value="">Month</option>
                                {#each [['01','January'],['02','February'],['03','March'],['04','April'],['05','May'],['06','June'],['07','July'],['08','August'],['09','September'],['10','October'],['11','November'],['12','December']] as [val, lbl] (val)}
                                    <option value={val}>{lbl}</option>
                                {/each}
                            </select>
                            <input type="number" value={dateInputStr.split('-')[0] ?? ''} onblur={(e) => { const mo = dateInputStr.split('-')[1] || '01'; dateInputStr = `${(e.target as HTMLInputElement).value}-${mo}`; resourceInfo.publicationDate = toIsoDate(dateInputStr, 'Month'); }} min="1900" max="2100" placeholder="YYYY" class="h-9 w-24 rounded-md border border-input bg-transparent px-3 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring" />
                        </div>
                    {:else if resourceInfo.publicationDatePrecision === 'Day'}
                        <input type="date" bind:value={dateInputStr} onblur={() => resourceInfo.publicationDate = toIsoDate(dateInputStr, 'Day')} class="h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring" />
                    {/if}
                </div>

                <!-- Abstract -->
                {#if mode === 'file'}
                    <div class="flex flex-col gap-1.5">
                        <Label>Abstract</Label>
                        <Textarea bind:value={resourceInfo.abstract} rows={3} />
                    </div>
                {/if}

                <!-- Description -->
                <div class="flex flex-col gap-1.5">
                    <Label>Description</Label>
                    <Textarea bind:value={resourceInfo.description} rows={3} />
                </div>

                <!-- Tags -->
                <div class="flex flex-col gap-1.5">
                    <Label>Tags</Label>
                    {#if tags.length > 0}
                        <div class="flex flex-wrap gap-1.5 py-2">
                            {#each tags as tag, i (tag)}
                                <span class="flex items-center gap-1 rounded-md bg-secondary px-2 py-0.5 text-xs">
                                    {tag}
                                    <button onclick={() => tags = tags.filter((_, j) => j !== i)} class="cursor-pointer text-muted-foreground hover:text-foreground px-0.5 -mr-1">×</button>
                                </span>
                            {/each}
                        </div>
                    {/if}
                    <Input
                        placeholder="Add tag..."
                        onkeydown={(e) => {
                            if (e.key === 'Enter') {
                                e.preventDefault();
                                const val = (e.currentTarget as HTMLInputElement).value.trim();
                                if (val && !tags.includes(val)) tags = [...tags, val];
                                (e.currentTarget as HTMLInputElement).value = '';
                            }
                        }}
                    />
                </div>

                <!-- Publication Code -->
                <div class="flex flex-col gap-1.5">
                    <Label>Publication Code</Label>
                    <Input bind:value={resourceInfo.publicationCode} />
                </div>

                <!-- License -->
                <div class="flex flex-col gap-1.5">
                    <Label>License</Label>
                    <Input bind:value={resourceInfo.license} />
                </div>

                <!-- Source URL -->
                <div class="flex flex-col gap-1.5">
                    <Label>Source URL</Label>
                    <Input type="url" bind:value={resourceInfo.sourceUrl} />
                </div>

                <!-- Note -->
                <div class="flex flex-col gap-1.5">
                    <Label>Note</Label>
                    <Textarea bind:value={resourceInfo.note} rows={3} />
                </div>
            </div>

            <!-- Connections -->
            <div class="flex flex-col gap-4 overflow-y-auto px-4 py-2">
                <h2 class="text-sm font-semibold">Connections</h2>

                <!-- Authors -->
                <div class="flex flex-col gap-2">
                    <div class="flex items-center justify-between">
                        <h3 class="text-xs font-medium text-muted-foreground uppercase tracking-wide">Authors</h3>
                        <button onclick={() => authors = [...authors, { extracted: '', value: '', displayValue: '', authorType: 'Person' }]} class="text-xs text-muted-foreground hover:text-foreground cursor-pointer">+ Add</button>
                    </div>
                    {#each authors as entry, i (i)}
                        <div class="flex flex-col gap-0.5">
                            {#if entry.extracted}
                                <span class="text-xs text-muted-foreground">{entry.extracted}</span>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <button
                                    onclick={() => { entry.authorType = entry.authorType === 'Organisation' ? 'Person' : 'Organisation'; entry.value = ''; entry.displayValue = ''; }}
                                    title={entry.authorType === 'Organisation' ? 'Organisation' : 'Person'}
                                    class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    {#if entry.authorType === 'Organisation'}
                                        <Building2 size={16} />
                                    {:else}
                                        <User size={16} />
                                    {/if}
                                </button>
                                <div class="flex-1">
                                    <AsyncSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={entry.authorType === 'Organisation' ? searchOrganisations : searchPersons}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span class="text-xs text-muted-foreground bg-secondary rounded px-1.5 py-0.5 shrink-0">New</span>
                                {/if}
                                <button onclick={() => authors = authors.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                        </div>
                    {/each}
                </div>

                <div class="border-t border-border"></div>

                <!-- Related Persons -->
                <div class="flex flex-col gap-2">
                    <div class="flex items-center justify-between">
                        <h3 class="text-xs font-medium text-muted-foreground uppercase tracking-wide">Related Persons</h3>
                        <button onclick={() => relatedPersons = [...relatedPersons, { extracted: '', value: '', displayValue: '' }]} class="text-xs text-muted-foreground hover:text-foreground cursor-pointer">+ Add</button>
                    </div>
                    {#each relatedPersons as entry, i (i)}
                        <div class="flex flex-col gap-0.5">
                            {#if entry.extracted}
                                <span class="text-xs text-muted-foreground">{entry.extracted}</span>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <div class="flex-1">
                                    <AsyncSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={searchPersons}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span class="text-xs text-muted-foreground bg-secondary rounded px-1.5 py-0.5 shrink-0">New</span>
                                {/if}
                                <button onclick={() => relatedPersons = relatedPersons.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                            {#if entry.value}
                                <Input bind:value={entry.role} placeholder="Role (optional)..." class="h-7 text-xs" />
                            {/if}
                        </div>
                    {/each}
                </div>

                <div class="border-t border-border"></div>

                <!-- Related Organisations -->
                <div class="flex flex-col gap-2">
                    <div class="flex items-center justify-between">
                        <h3 class="text-xs font-medium text-muted-foreground uppercase tracking-wide">Related Organisations</h3>
                        <button onclick={() => organisations = [...organisations, { extracted: '', value: '', displayValue: '' }]} class="text-xs text-muted-foreground hover:text-foreground cursor-pointer">+ Add</button>
                    </div>
                    {#each organisations as entry, i (i)}
                        <div class="flex flex-col gap-0.5">
                            {#if entry.extracted}
                                <span class="text-xs text-muted-foreground">{entry.extracted}</span>
                            {/if}
                            <div class="flex gap-1.5 items-center">
                                <div class="flex-1">
                                    <AsyncSelect
                                        bind:value={entry.value}
                                        bind:displayValue={entry.displayValue}
                                        search={searchOrganisations}
                                        oncreate={async (name) => ({ id: name, name })}
                                        placeholder="Search or create..."
                                    />
                                </div>
                                {#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
                                    <span class="text-xs text-muted-foreground bg-secondary rounded px-1.5 py-0.5 shrink-0">New</span>
                                {/if}
                                <button onclick={() => organisations = organisations.filter((_, j) => j !== i)} class="text-muted-foreground hover:text-foreground shrink-0 cursor-pointer">
                                    <X size={14} />
                                </button>
                            </div>
                            {#if entry.value}
                                <Input bind:value={entry.role} placeholder="Role (optional)..." class="h-7 text-xs" />
                            {/if}
                        </div>
                    {/each}
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
                <!-- Mode toggle -->
                <div class="flex rounded-lg bg-muted p-1">
                    <button
                        type="button"
                        onclick={() => (mode = 'file')}
                        class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode ===
                        'file'
                            ? 'bg-backround shadow-sm'
                            : 'text-muted-foreground hover:text-foreground'}"
                    >
                        File
                    </button>
                    <button
                        type="button"
                        onclick={() => (mode = 'url')}
                        class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode ===
                        'url'
                            ? 'bg-backround shadow-sm'
                            : 'text-muted-foreground hover:text-foreground'}"
                    >
                        URL
                    </button>
                </div>

                {#if mode === 'file'}
                    <!-- Drop zone -->
                    <div
                        role="button"
                        tabindex="0"
                        onclick={() => fileInput?.click()}
                        onkeydown={(e) => e.key === 'Enter' && fileInput?.click()}
                        ondragover={(e) => {
                            e.preventDefault();
                            isDragging = true;
                        }}
                        ondragleave={() => (isDragging = false)}
                        ondrop={(e) => {
                            e.preventDefault();
                            isDragging = false;
                            selectFile(e.dataTransfer?.files[0]);
                        }}
                        class="flex min-h-48 cursor-pointer flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed border-border p-8 transition-colors {isDragging
                            ? 'border-primary bg-accent'
                            : 'hover:bg-accent/50'}"
                    >
                        {#if selectedFile}
                            <FileText class="h-8 w-8 text-primary" />
                            <p class="text-sm font-medium">{selectedFile.name}</p>
                            <p class="text-xs text-muted-foreground">
                                {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                            </p>
                            <button
                                onclick={(e) => {
                                    e.stopPropagation();
                                    selectedFile = null;
                                }}
                                class="text-xs text-muted-foreground underline hover:text-foreground">Remove</button
                            >
                        {:else}
                            <Upload class="h-8 w-8 text-muted-foreground" />
                            <p class="text-sm font-medium">Drop a file or click to browse</p>
                            <p class="text-xs text-muted-foreground">{supportedExtensions.join(', ')}</p>
                        {/if}
                    </div>

                    {#if fileError}
                        <p class="text-xs text-destructive">{fileError}</p>
                    {/if}

                    <input
                        bind:this={fileInput}
                        type="file"
                        class="hidden"
                        accept={supportedExtensions.map((ext) => `.${ext}`).join(',')}
                        onchange={() => selectFile(fileInput?.files?.[0])}
                    />
                {:else}
                    <!-- URL input -->
                    <div class="flex flex-col gap-1.5">
                        <Label for="url">URL</Label>
                        <Input id="url" type="url" bind:value={url} placeholder="https://example.com/article" />
                    </div>
                {/if}

                {#if startError}
                    <p class="text-xs text-destructive">{startError}</p>
                {/if}

                <!-- Next -->
                <div class="flex justify-end">
                    <Button disabled={!canProceed || isStarting} onclick={handleNext} class="cursor-pointer">
                        {#if uploadStep === 'hashing'}Hashing...
                        {:else if uploadStep === 'uploading'}Uploading...
                        {:else if uploadStep === 'starting'}Starting...
                        {:else}Next{/if}
                    </Button>
                </div>
            {:else if phase === 'processing'}
                <div class="flex flex-col items-center gap-4 py-8">
                    <Spinner class="h-8 w-8" />
                    <p class="text-sm font-medium">{statusMessage || 'Processing...'}</p>
                    <p class="text-muted-foreground text-xs">{progressPercentage}%</p>
                    {#if processingError}
                        <p class="text-destructive text-xs">{processingError}</p>
                    {/if}
                </div>

                <div class="flex justify-center">
                    <Button variant="ghost" class="cursor-pointer" onclick={() => { phase = 'review'; extractedMetadata = null; }}>
                        Skip to manual entry
                    </Button>
                </div>
            {:else if phase === 'duplicate'}
                <div class="flex flex-col gap-4 border-border rounded-lg border p-6">
                    <div class="flex flex-col gap-1.5">
                        <p class="text-sm font-medium">This resource already exists in the library.</p>
                        <p class="text-muted-foreground text-xs">You can view the existing resource or go back to upload a different file.</p>
                    </div>

                    <div class="flex gap-3">
                        <Button variant="outline" onclick={() => phase = 'select'}>Go back</Button>
                        <Button href="/library?inspectorId={duplicateId}&inspectorType=resource">View Resource</Button>
                    </div>
                </div>
            {/if}
        </div>
    </div>
{/if}