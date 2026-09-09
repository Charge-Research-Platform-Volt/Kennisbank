<script lang="ts">
	import Input from '$lib/components/ui/input/input.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { api } from '$lib/api';
	import { toast } from 'svelte-sonner';

	let email = $state('');
	let loading = $state(false);
	let sent = $state(false);

	async function handleSubmit(e: SubmitEvent) {
		e.preventDefault();
		loading = true;

		try {
			await api.post('/api/auth/forgot-password', { email });
			sent = true;
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Something went wrong, please try again.');
		} finally {
			loading = false;
		}
	}
</script>

<div class="w-full max-w-lg px-8 py-4">
	{#if sent}
		<div class="flex flex-col gap-1">
			<h1 class="text-3xl font-bold">Check your email</h1>
			<p>If that email is registered, we've sent a link to reset your password.</p>
		</div>

		<p class="mt-4 text-center text-muted-foreground">
			<a href="/login" class="text-blue-500">Back to login</a>
		</p>
	{:else}
		<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
			<div class="flex flex-col gap-1">
				<h1 class="text-3xl font-bold">Forgot password</h1>
				<p>Enter your email and we'll send you a reset link.</p>
			</div>

			<div class="flex flex-col gap-1">
				<label for="email" class="font-bold">Email</label>
				<Input bind:value={email} type="email" name="email" id="email" required />
			</div>

			<Button type="submit" class="w-full cursor-pointer" disabled={!email || loading}>
				{#if loading}
					Sending...
				{:else}
					Send reset link
				{/if}
			</Button>

			<p class="text-center text-muted-foreground">
				<a href="/login" class="text-blue-500">Back to login</a>
			</p>
		</form>
	{/if}
</div>
