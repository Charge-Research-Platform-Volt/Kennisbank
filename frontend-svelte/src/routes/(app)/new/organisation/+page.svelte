<script lang="ts">
    import { goto } from '$app/navigation';
    import { debounce } from '$lib/utils/debounce';
    import { api } from '$lib/api';
    import Input from '$lib/components/ui/input/input.svelte';
    import Button from '$lib/components/ui/button/button.svelte';
    import Textarea from '$lib/components/ui/textarea/textarea.svelte';
    import Label from '$lib/components/ui/label/label.svelte';
    import { toast } from 'svelte-sonner';

    let name = $state('');
    let description = $state('');
    let email = $state('');
    let website = $state('');

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
            const result = await api.get<{ exists: boolean; id: string; }>(`/api/organisations/exists?name=${encodeURIComponent(name)}`);
            duplicate = result.body;

            const listResult = await api.get<{ id: string; name: string; }[]>(`/api/organisations/list?searchQuery=${encodeURIComponent(name)}&pageSize=3&properties=Id,Name`);
            similar = listResult.body.filter(o => o.name.toLowerCase() !== name.toLowerCase());
        } catch {
            // Silently ignore
        } finally {
            isCheckingDuplicate = false;
        }
    }

    async function handleSubmit(e: SubmitEvent) {
        e.preventDefault();

        if (!name.trim()) return;

        isSubmitting = true;
        try {
            const result = await api.put<string>('/api/organisations/new', {
                Name: name,
                Description: description || undefined,
                EmailAddress: email || undefined,
                Website: website || undefined
            });

            goto(`/library?inspectorId=${result.body}&inspectorType=organisation`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to create organisation');
        } finally {
            isSubmitting = false;
        }
    }

    const canSubmit = $derived(name.trim().length > 0 && !isSubmitting && !duplicate?.exists);
</script>

<div class="mx-auto flex max-w-2xl flex-col gap-6 p-8 h-full justify-center">
    <!-- Header -->
    <div>
        <h1 class="text-2xl font-semibold">Add Organisation</h1>
        <p class="text-muted-foreground mt-1 text-sm">Add a new organisation to the library.</p>
    </div>

    <!-- Form -->
    <form onsubmit={handleSubmit} class="flex flex-col gap-6">
        <!-- Card -->
        <div class="border-border flex flex-col gap-4 rounded-lg border p-6">
            <!-- Name (Required) -->
            <div class="flex flex-col gap-1.5">
                <Label for="name">Name <span class="text-destructive">*</span></Label>
                <Input id="name" bind:value={name} oninput={onNameInput} placeholder="Organisation name" />
                {#if isCheckingDuplicate}
                    <p class="text-muted-foreground text-xs">Checking for duplicates...</p>
                {:else if duplicate?.exists}
                    <p class="text-destructive text-xs">
                        An organisation with this name already exists.
                        <a href="/library?inspectorId={duplicate.id}&inspectorType=organisation" class="underline">View</a>
                    </p>
                {:else if similar.length > 0}
                    <p class="text-muted-foreground text-xs">
                        Similar names in the library:
                        {#each similar as s, i (s.id)}
                            <a href="/library?inspectorId={s.id}&inspectorType=organisation" class="underline">{s.name}</a>{#if i < similar.length - 1},&nbsp;{/if}
                        {/each}
                    </p>
                {/if}
            </div>

            <!-- Email -->
            <div class="flex flex-col gap-1.5">
                <Label for="email">Email</Label>
                <Input id="email" type="email" bind:value={email} placeholder="contact@example.com" />
            </div>

            <!-- Website -->
            <div class="flex flex-col gap-1.5">
                <Label for="website">Website</Label>
                <Input id="website" type="url" bind:value={website} placeholder="https://example.com" />
            </div>

            <!-- Description -->
            <div class="flex flex-col gap-1.5">
                <Label for="description">Description</Label>
                <Textarea id="description" bind:value={description} placeholder="Brief description of the organisation" rows={4} />
            </div>
        </div>


        <!-- Actions -->
        <div class="flex justify-end gap-3">
            <Button variant="outline" type="button" onclick={() => history.back()}>Cancel</Button>
            <Button type="submit" disabled={!canSubmit}>
                {isSubmitting ? 'Creating...' : 'Create Organisation'}
            </Button>
        </div>
    </form>
</div>
