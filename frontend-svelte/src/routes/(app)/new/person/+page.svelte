<script lang="ts">
    import { goto } from '$app/navigation';
    import { debounce } from '$lib/utils/debounce';
    import { api } from '$lib/api';
    import Input from '$lib/components/ui/input/input.svelte';
    import Button from '$lib/components/ui/button/button.svelte';
    import Textarea from '$lib/components/ui/textarea/textarea.svelte';
    import Label from '$lib/components/ui/label/label.svelte';
    import { toast } from 'svelte-sonner';
    import type { ProjectListResponse } from '$lib/types/project';
    import { onMount } from 'svelte';
    import { page } from '$app/state';
    import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';

    let name = $state('');
    let occupation = $state('');
    let description = $state('');
    let email = $state('');
    let linkedin = $state('');
    let sourceProjectId = $state<string | null>(null);
    let projectIds = $state<string[]>([]);

    let isSubmitting = $state(false);
    let isCheckingDuplicate = $state(false);
    let duplicate = $state<{ exists: boolean; id: string; } | null>(null);
    let similar = $state<{ id: string; name: string; }[]>([]);

    const debouncedCheckDuplicate = debounce(checkDuplicate, 500);

    function onNameInput() {
        duplicate = null;
        similar = [];
        if (!name.trim()) return;
        debouncedCheckDuplicate();
    }

    async function checkDuplicate() {
        isCheckingDuplicate = true;

        try {
            const result = await api.get<{ exists: boolean; id: string; }>(`/api/persons/exists?name=${encodeURIComponent(name)}`);
            duplicate = result.body;

            const listResult = await api.get<{ id: string; name: string; }[]>(`/api/persons/list?searchQuery=${encodeURIComponent(name)}&pageSize=3&properties=Id,Name`);
            similar = listResult.body.filter(p => p.name.toLowerCase() !== name.toLowerCase());
        }catch {
            // Silently ignore
        }finally {
            isCheckingDuplicate = false;
        }
    }

    async function handleSubmit(e: SubmitEvent) {
        e.preventDefault();

        if (!name.trim()) return;

        isSubmitting = true;
        try {
            const result = await api.put<string>('/api/persons/new', {
                Name: name,
                Occupation: occupation || undefined,
                Description: description || undefined,
                EmailAddress: email || undefined,
                Linkedin: linkedin || undefined
            });

            // Add to selected projects
            if (projectIds.length > 0) {
                const results = await Promise.allSettled(
                    projectIds.map(pid => api.put(`/api/project/add-item/${pid}/${result.body}`, {}))
                );

                const failed = results.filter(r => r.status === 'rejected').length;
                if (failed > 0) toast.error(`Added person, but failed to link ${failed} project${failed > 1 ? 's' : ''}.`);
                else toast.success(`Linked person to ${results.length} project${results.length > 1 ? 's' : ''}.`);
            }

            if (sourceProjectId)
                goto(`/projects/${sourceProjectId}?inspectorId=${result.body}&inspectorType=person`);
            else
                goto(`/library?inspectorId=${result.body}&inspectorType=person`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to create person');
        } finally {
            isSubmitting = false;
        }
    }

    async function searchProjects(q: string) {
        const result = await api.post<ProjectListResponse>('/api/project/list', {
            usePaging: true,
            pageIndex: 1,
            pageSize: 20,
            searchQuery: q || undefined
        });

        return result.body.projects.map(p => ({ id: p.id, name: p.title }));
    }

    const canSubmit = $derived(name.trim().length > 0 && !isSubmitting && !duplicate?.exists);

    onMount(() => {
        const paramId = page.url.searchParams.get('projectId');
        if (paramId) {
            projectIds = [paramId];
            sourceProjectId = paramId;
        }
    });
</script>

<div class="mx-auto flex max-w-2xl flex-col gap-6 p-8 h-full justify-center">
    <!-- Header -->
    <div>
        <h1 class="text-2xl font-semibold">Add Person</h1>
        <p class="text-muted-foreground mt-1 text-sm">Add a new person to the library.</p>
    </div>

    <!-- Form -->
    <form onsubmit={handleSubmit} class="flex flex-col gap-6">
        <!-- Card -->
        <div class="border-border flex flex-col gap-4 rounded-lg border p-6">
            <!-- Name (Required) -->
            <div class="flex flex-col gap-1.5">
                <Label for="name">Name <span class="text-destructive">*</span></Label>
                <Input id="name" bind:value={name} oninput={onNameInput} placeholder="Full name" />
                {#if isCheckingDuplicate}
                    <p class="text-muted-foreground text-xs">Checking for duplicates...</p>
                {:else if duplicate?.exists}
                    <p class="text-destructive text-xs">
                        A person with this name already exists.
                        <a href="/library?inspectorId={duplicate.id}&inspectorType=person" class="underline">View</a>
                    </p>
                {:else if similar.length > 0}
                    <p class="text-muted-foreground text-xs">
                        Similar names already in the library:
                        {#each similar as s, i (s.id)}
                            <a href="/libary?inspectorId={s.id}&inspectorType=person" class="underline">{s.name}</a>{#if i < similar.length - 1},&nbsp;{/if}
                        {/each}
                    </p>
                {/if}
            </div>

            <!-- Occupation -->
            <div class="flex flex-col gap-1.5">
                <Label for="occupation">Occupation</Label>
                <Input id="occupation" bind:value={occupation} placeholder="e.g. Politician" />
            </div>

            <!-- Email -->
            <div class="flex flex-col gap-1.5">
                <Label for="email">Email</Label>
                <Input id="email" type="email" bind:value={email} placeholder="email@example.com" />
            </div>

            <!-- LinkedIn -->
            <div class="flex flex-col gap-1.5">
                <Label for="linkedin">LinkedIn</Label>
                <Input id="linkedin" type="url" bind:value={linkedin} placeholder="https://linkedin.com/in/username" />
            </div>

            <!-- Description -->
            <div class="flex flex-col gap-1.5">
                <Label for="description">Description</Label>
                <Textarea id="description" bind:value={description} placeholder="Brief biography or description" rows={4} />
            </div>

            <!-- Add to project -->
            <div class="flex flex-col gap-1.5">
                <Label>Add to projects</Label>
                <AsyncMultiSelect bind:value={projectIds} search={searchProjects} placeholder="Add to projects..." />
            </div>
        </div>


        <!-- Actions -->
        <div class="flex justify-end gap-3">
            <Button variant="outline" type="button" onclick={() => history.back()} class="cursor-pointer">Cancel</Button>
            <Button type="submit" disabled={!canSubmit} class="cursor-pointer">
                {isSubmitting ? 'Creating...' : 'Create Person'}
            </Button>
        </div>
    </form>
</div>
