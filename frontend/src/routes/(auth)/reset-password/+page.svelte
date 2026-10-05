<script lang="ts">
	import Input from '$lib/components/ui/input/input.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '$lib/api';
	import PasswordRequirements from '$lib/components/ui/password-requirements.svelte';
	import { isPasswordValid } from '$lib/utils/password';
	import { toast } from 'svelte-sonner';

	const email = page.url.searchParams.get('email') ?? '';
	const token = page.url.searchParams.get('token') ?? '';

	let newPassword = $state('');
	let newPasswordRepeat = $state('');
	let passwordValid = $derived(isPasswordValid(newPassword));
	let loading = $state(false);

	async function handleSubmit(e: SubmitEvent) {
		e.preventDefault();

		if (newPassword !== newPasswordRepeat) {
			toast.error("Passwords don't match.");
			return;
		}

		loading = true;

		try {
			await api.post('/api/auth/reset-password', { email, token, newPassword });
			toast.success('Password updated. You can now log in.');
			goto('/login');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'This reset link is invalid or has expired.');
		} finally {
			loading = false;
		}
	}
</script>

<div class="w-full max-w-lg px-8 py-4">
	<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
		<div class="flex flex-col gap-1">
			<h1 class="text-3xl font-bold">Reset password</h1>
			<p>Choose a new password for {email}.</p>
		</div>

		<div class="flex flex-col gap-1">
			<label for="newPassword" class="font-bold">New password</label>
			<Input bind:value={newPassword} type="password" id="newPassword" required />
			<PasswordRequirements password={newPassword} />
		</div>

		<div class="flex flex-col gap-1">
			<label for="newPasswordRepeat" class="font-bold">Repeat new password</label>
			<Input bind:value={newPasswordRepeat} type="password" id="newPasswordRepeat" required />
		</div>

		<Button
			type="submit"
			class="w-full cursor-pointer"
			disabled={!passwordValid || !newPasswordRepeat || !email || !token || loading}
		>
			{#if loading}
				Resetting...
			{:else}
				Reset password
			{/if}
		</Button>
	</form>
</div>
