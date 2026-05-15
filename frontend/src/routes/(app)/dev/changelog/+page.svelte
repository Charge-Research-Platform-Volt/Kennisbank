<script lang="ts">
	import { api } from '$lib/api';
	import Button from '$lib/components/ui/button/button.svelte';
	import Input from '$lib/components/ui/input/input.svelte';

	let title = $state('');
	let body = $state('');
	let status = $state('');

	async function submit() {
		try {
			await api.post('/api/changelog', { title, body });
			title = '';
			body = '';
			status = 'Entry added.';
		} catch (e) {
			status = `Error: ${e instanceof Error ? e.message : 'Unknown error'}`;
		}
	}
</script>

<div class="flex max-w-xl flex-col gap-4 p-8">
	<h1 class="text-lg font-semibold">Add Changelog Entry</h1>

	<div class="flex flex-col gap-1.5">
		<label class="text-sm font-medium" for="title">Title</label>
		<Input id="title" bind:value={title} placeholder="What changed?" />
	</div>

	<div class="flex flex-col gap-1.5">
		<label class="text-sm font-medium" for="body">Body</label>
		<textarea
			id="body"
			bind:value={body}
			placeholder="Describe the change..."
			rows="5"
			class="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
		></textarea>
	</div>

	<div class="flex items-center gap-4">
		<Button class="cursor-pointer" onclick={submit} disabled={!title || !body}>Submit</Button>
		{#if status}
			<span class="text-sm text-muted-foreground">{status}</span>
		{/if}
	</div>
</div>
