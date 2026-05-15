<script lang="ts">
	import { userState } from '$lib/state/user.svelte';
	import { api } from '$lib/api';
	import Avatar from '$lib/components/ui/avatar/avatar.svelte';
	import Input from '$lib/components/ui/input/input.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import Label from '$lib/components/ui/label/label.svelte';
	import { toast } from 'svelte-sonner';
	import { goto } from '$app/navigation';
	import { Pencil } from '@lucide/svelte';
	import { confirm } from '$lib/state/confirm.svelte';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import PasswordRequirements from '$lib/components/ui/password-requirements.svelte';
	import { isPasswordValid } from '$lib/utils/password';

	// Avatar
	let avatarFile = $state<File | null>(null);
	let avatarPreviewUrl = $derived(avatarFile ? URL.createObjectURL(avatarFile) : null);
	let avatarInputElement = $state<HTMLInputElement | null>(null);
	let loadingAvatar = $state(false);

	// Details
	let firstName = $state(userState.user?.firstName ?? '');
	let lastName = $state(userState.user?.lastName ?? '');
	let email = $state(userState.user?.email ?? '');
	let loadingDetails = $state(false);

	// Password
	let currentPassword = $state('');
	let newPassword = $state('');
	let newPasswordRepeat = $state('');
	let loadingPassword = $state(false);

	// Delete
	let loadingDelete = $state(false);

	async function handleAvatarChange() {
		const file = avatarInputElement?.files?.[0];

		if (!file) return;

		loadingAvatar = true;
		avatarFile = file;

		const formData: FormData = new FormData();
		formData.append('newAvatar', file);

		try {
			const response = await api.form<number>('/api/user/avatar', formData);

			userState.user = { ...userState.user!, customAvatarVersion: response.body };

			toast.success('Avatar updated successfully.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update avatar.');
		} finally {
			loadingAvatar = false;
		}
	}

	async function removeAvatar() {
		loadingAvatar = true;

		try {
			await api.delete('/api/user/avatar');
			userState.user = { ...userState.user!, customAvatarVersion: null };

			avatarFile = null;

			toast.success('Avatar removed successfully.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to remove avatar.');
		} finally {
			loadingAvatar = false;
		}
	}

	async function saveDetails() {
		loadingDetails = true;

		try {
			await api.patch('/api/user/update-details', {
				newFirstName: firstName,
				newLastName: lastName,
				newEmail: email
			});

			userState.user = {
				...userState.user!,
				firstName: firstName,
				lastName: lastName,
				email: email
			};

			toast.success('Successfully updated account details.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update account details.');
		} finally {
			loadingDetails = false;
		}
	}

	async function savePassword() {
		if (newPassword !== newPasswordRepeat) {
			toast.error("Passwords don't match.");
			return;
		}

		if (newPassword === currentPassword) {
			toast.warning('Password is the same as current password.');
			return;
		}

		if (!isPasswordValid(newPassword)) {
			toast.error('The password is not valid.');
			return;
		}

		loadingPassword = true;

		try {
			await api.put('/api/auth/update-password', {
				currentPassword,
				newPassword
			});

			currentPassword = '';
			newPassword = '';
			newPasswordRepeat = '';

			toast.success('Successfully updated password.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update password.');
		} finally {
			loadingPassword = false;
		}
	}

	async function deleteAccount() {
		const confirmed: boolean = await confirm(
			'This will permanently delete your account.',
			'DELETE'
		);

		if (!confirmed) return;

		loadingDelete = true;

		try {
			await api.delete('/api/user/delete');

			goto('/login');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete account.');
		} finally {
			loadingDelete = false;
		}
	}
</script>

<div class="mx-auto flex max-w-2xl flex-col gap-8 p-8">
	<section class="flex flex-col gap-3">
		<div>
			<h2 class="text-xl font-semibold">Profile</h2>
			<p class="text-sm text-muted-foreground">Update your name, email and avatar</p>
		</div>

		<div class="rounded-lg border p-6">
			<form
				onsubmit={(e) => {
					e.preventDefault();
					saveDetails();
				}}
				class="flex flex-col gap-4"
			>
				<!-- Avatar + fields row -->
				<div class="flex gap-6">
					<!-- Avatar -->
					<div class="flex shrink-0 flex-col items-center gap-2">
						<button
							type="button"
							onclick={() => avatarInputElement?.click()}
							class="group relative cursor-pointer"
						>
							{#if loadingAvatar}
								<div
									style="width: 96px; height: 96px;"
									class="justify-cetner flex items-center rounded-full bg-muted ring-2 ring-background"
								>
									<Spinner class="h-5 w-5" />
								</div>
							{:else}
								<Avatar
									userId={userState.user?.id ?? ''}
									name="{firstName} {lastName}"
									src={avatarPreviewUrl}
									customAvatarVersion={userState.user?.customAvatarVersion}
									size={96}
								/>

								<div
									class="absolute inset-0 flex items-center justify-center rounded-full bg-black/50 opacity-0 transition-opacity group-hover:opacity-100"
								>
									<Pencil class="text-white" size={16} />
								</div>
							{/if}
						</button>

						<input
							type="file"
							accept="image/png,image/jpeg,image/webp"
							bind:this={avatarInputElement}
							onchange={handleAvatarChange}
							hidden
						/>

						{#if userState.user?.customAvatarVersion !== null}
							<button
								type="button"
								onclick={removeAvatar}
								class="cursor-pointer text-xs text-muted-foreground transition-colors hover:text-destructive"
							>
								Remove
							</button>
						{/if}
					</div>

					<!-- Name + email -->
					<div class="flex min-w-0 flex-1 flex-col gap-3">
						<div class="flex gap-3">
							<div class="flex flex-1 flex-col gap-1.5">
								<Label for="firstName">First name</Label>
								<Input id="firstName" bind:value={firstName} required />
							</div>

							<div class="flex flex-1 flex-col gap-1.5">
								<Label for="lastName">Last name</Label>
								<Input id="lastname" bind:value={lastName} required />
							</div>
						</div>

						<div class="flex flex-col gap-1.5">
							<Label for="email">Email</Label>
							<Input id="email" type="email" bind:value={email} required />
						</div>
					</div>
				</div>

				<div class="flex justify-end">
					<Button
						type="submit"
						disabled={loadingDetails || !firstName || !lastName || !email}
						class="cursor-pointer"
					>
						{loadingDetails ? 'Saving...' : 'Save'}
					</Button>
				</div>
			</form>
		</div>
	</section>

	<section class="flex flex-col gap-3">
		<div>
			<h2 class="text-xl font-semibold">Password</h2>
			<p class="text-sm text-muted-foreground">Change your password</p>
		</div>

		<div class="rounded-lg border p-6">
			<form
				onsubmit={(e) => {
					e.preventDefault();
					savePassword();
				}}
				class="flex flex-col gap-4"
			>
				<div class="flex flex-col gap-1.5">
					<Label for="currentPassword">Current password</Label>
					<Input id="currentPassword" type="password" bind:value={currentPassword} required />
				</div>

				<hr class="mx-auto w-full" />

				<div class="flex flex-col gap-1.5">
					<Label for="newPassword">New password</Label>
					<Input id="newPassword" type="password" bind:value={newPassword} required />

					<PasswordRequirements password={newPassword} />
				</div>

				<div class="flex flex-col gap-1.5">
					<Label for="newPasswordRepeat">Repeat new password</Label>
					<Input id="newPasswordRepeat" type="password" bind:value={newPasswordRepeat} required />
				</div>

				<div class="flex justify-end">
					<Button
						type="submit"
						disabled={loadingPassword ||
							!isPasswordValid(newPassword) ||
							!currentPassword ||
							!newPassword ||
							!newPasswordRepeat}
						class="cursor-pointer"
					>
						{loadingPassword ? 'Saving...' : 'Save'}
					</Button>
				</div>
			</form>
		</div>
	</section>

	<section class="flex flex-col gap-3">
		<div>
			<h2 class="text-xl font-semibold">Danger zone</h2>
			<p class="text-sm text-muted-foreground">Permanently delete your account.</p>
		</div>

		<div class="flex items-center justify-between rounded-lg border border-destructive p-6">
			<p class="text-sm text-muted-foreground">This action is permanent and cannot be undone.</p>
			<Button
				variant="destructive"
				onclick={deleteAccount}
				disabled={loadingDelete}
				class="cursor-pointer"
			>
				{loadingDelete ? 'Deleting...' : 'Delete account'}
			</Button>
		</div>
	</section>
</div>
