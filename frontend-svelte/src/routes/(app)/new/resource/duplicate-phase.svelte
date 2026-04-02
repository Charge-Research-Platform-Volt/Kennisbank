<script lang="ts">
    import Button from '$lib/components/ui/button/button.svelte';

    let { duplicateId, onback }: {
        duplicateId: string;
        onback: () => void;
    } = $props();
</script>

<div class="flex flex-col gap-4 border-border rounded-lg border p-6">
    <div class="flex flex-col gap-1.5">
        <p class="text-sm font-medium">This resource already exists in the library.</p>
        <p class="text-muted-foreground text-xs">You can view the existing resource or go back to upload a different file.</p>
    </div>

    <div class="flex gap-3">
        <Button variant="outline" onclick={onback}>Go back</Button>
        <Button href="/library?inspectorId={duplicateId}&inspectorType=resource">View Resource</Button>
    </div>
</div>
