<script lang="ts">
	import { getContext, untrack } from 'svelte';
	import type { DatePrecision } from '$lib/types/resource';
	import { formatDate } from '$lib/utils/date';

	let {
		date,
		precision,
		onsave
	}: {
		date?: string;
		precision?: DatePrecision;
		onsave: (date: string | null, precision: DatePrecision) => Promise<void>;
	} = $props();

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());
	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	function initFromDate() {
		if (!date) return;
		const d = new Date(date);
		dateYear = String(d.getFullYear());
		dateMonth = precision !== 'Year' ? String(d.getMonth() + 1) : '';
		dateDay = precision === 'Day' ? String(d.getDate()) : '';
	}

	let dateDay = $state('');
	let dateMonth = $state('');
	let dateYear = $state('');

	untrack(() => initFromDate());

	function updateDate() {
		let isoDate: string | null;
		let prec: DatePrecision;
		if (dateYear && dateMonth && dateDay) {
			prec = 'Day';
			isoDate = `${dateYear}-${dateMonth.padStart(2, '0')}-${dateDay.padStart(2, '0')}`;
		} else if (dateYear && dateMonth) {
			prec = 'Month';
			isoDate = `${dateYear}-${dateMonth.padStart(2, '0')}-01`;
		} else if (dateYear) {
			prec = 'Year';
			isoDate = `${dateYear}-01-01`;
		} else {
			prec = 'Year';
			isoDate = null;
		}
		registerSave?.(onsave(isoDate, prec));
	}
</script>

{#if editMode}
	<div
		class="flex flex-col gap-0.5 border-b border-transparent pb-0.5 transition-colors focus-within:border-border"
	>
		<span class="text-xs text-muted-foreground">Publish Date</span>
		<div class="flex items-center gap-1 text-sm">
			<input
				type="number"
				bind:value={dateDay}
				onblur={updateDate}
				min="1"
				max="31"
				placeholder="DD"
				class="w-8 bg-transparent outline-none placeholder:text-muted-foreground/50"
			/>
			<span class="text-muted-foreground/30">/</span>
			<input
				type="number"
				bind:value={dateMonth}
				onblur={updateDate}
				min="1"
				max="12"
				placeholder="MM"
				class="w-8 bg-transparent outline-none placeholder:text-muted-foreground/50"
			/>
			<span class="text-muted-foreground/30">/</span>
			<input
				type="number"
				bind:value={dateYear}
				onblur={updateDate}
				min="1000"
				max="2100"
				placeholder="YYYY"
				class="w-14 bg-transparent outline-none placeholder:text-muted-foreground/50"
			/>
		</div>
	</div>
{:else}
	<span>Published: {formatDate(date ?? '', precision) ?? '-'}</span>
{/if}

<style>
	input[type='number']::-webkit-inner-spin-button,
	input[type='number']::-webkit-outer-spin-button {
		-webkit-appearance: none;
		appearance: none;
		margin: 0;
	}
	input[type='number'] {
		-moz-appearance: textfield;
		appearance: textfield;
	}
</style>
