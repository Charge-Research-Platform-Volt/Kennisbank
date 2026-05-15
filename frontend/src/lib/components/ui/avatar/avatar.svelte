<script lang="ts">
	let {
		userId = '',
		name = '',
		customAvatarVersion = null,
		src = null,
		size = 32,
		class: className = ''
	}: {
		userId?: string;
		name?: string;
		customAvatarVersion?: number | null;
		src?: string | null;
		size?: number;
		class?: string;
	} = $props();

	let imgError = $state(false);

	let resolvedSrc = $derived(
		src ??
			(customAvatarVersion != null && customAvatarVersion > 0
				? `/api/user/avatar/${userId}?v=${customAvatarVersion}`
				: null)
	);

	$effect(() => {
		resolvedSrc;
		imgError = false;
	});

	let initials = $derived(
		name
			.split(' ')
			.filter(Boolean)
			.map((n) => n[0])
			.slice(0, 2)
			.join('')
			.toUpperCase()
	);
</script>

{#if resolvedSrc && !imgError}
	<img
		src={resolvedSrc}
		alt={name}
		title={name}
		style="width: {size}px; height: {size}px;"
		class="shrink-0 rounded-full object-cover ring-2 ring-background {className}"
		onerror={() => (imgError = true)}
	/>
{:else}
	<div
		title={name}
		style="width: {size}px; height: {size}px; font-size: {size * 0.35}px;"
		class="flex shrink-0 items-center justify-center rounded-full bg-primary/15 font-medium text-primary ring-2 ring-background select-none {className}"
	>
		{initials || '?'}
	</div>
{/if}
