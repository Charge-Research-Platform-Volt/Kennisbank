<script lang="ts">
	import Avatar from '#lib/components/ui/avatar/avatar.svelte';
	import * as Tooltip from '#lib/components/ui/tooltip/index.js';
	import { formatDate } from '#lib/utils/date.js';
	import type { UserSummary } from '#lib/types/user.js';

	let {
		date,
		user,
		action = 'Added'
	}: {
		/** Null for records made before the date was kept; then only the person shows */
		date: string | null;
		user: UserSummary;
		/** What the person did, for the tooltip: "Added by …", "Uploaded by …" */
		action?: string;
	} = $props();
</script>

<div class="flex items-center justify-end gap-2 text-xs whitespace-nowrap text-muted-foreground">
	{#if date}
		<span>{formatDate(date)}</span>
	{/if}

	<Tooltip.Root>
		<Tooltip.Trigger>
			<Avatar
				userId={user.id ?? ''}
				name={user.name}
				customAvatarVersion={user.customAvatarVersion}
				size={22}
			/>
		</Tooltip.Trigger>
		<Tooltip.Content>
			<p>{action} by {user.name}</p>
		</Tooltip.Content>
	</Tooltip.Root>
</div>
