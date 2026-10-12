<script lang="ts">
	import * as Chart from '$lib/components/ui/chart';
	import { BarChart } from 'layerchart';
	import InfoTip from './InfoTip.svelte';

	let {
		label,
		dates,
		counts,
		detail = null,
		info = null
	}: {
		label: string;
		dates: string[];
		counts: number[];
		detail?: string | null;
		info?: string | null;
	} = $props();

	const config = { count: { label: 'Count', color: 'var(--primary)' } } satisfies Chart.ChartConfig;

	let data = $derived(dates.map((date, i) => ({ date, count: counts[i] ?? 0 })));
	let total = $derived(counts.reduce((sum, c) => sum + c, 0));
	// Only the first and last day get an axis label, the tooltip carries the rest
	let edgeTicks = $derived(dates.length > 1 ? [dates[0], dates[dates.length - 1]] : dates);

	function formatDay(date: string) {
		return new Date(`${date}T00:00:00Z`).toLocaleDateString(undefined, {
			day: 'numeric',
			month: 'short',
			timeZone: 'UTC'
		});
	}
</script>

<div class="flex flex-col gap-1 rounded-md border p-4">
	<span class="flex items-center gap-1.5 text-sm text-muted-foreground"
		>{label}<InfoTip text={info} /></span
	>
	<span class="text-2xl font-semibold">{total.toLocaleString()}</span>
	<span class="min-h-4 text-xs text-muted-foreground">{detail ?? ''}</span>

	<Chart.Container {config} class="mt-2 aspect-auto h-24 w-full">
		<BarChart
			{data}
			x="date"
			axis="x"
			grid={false}
			rule={false}
			bandPadding={0.2}
			series={[{ key: 'count', label, color: config.count.color }]}
			props={{
				bars: { rounded: 'top', radius: 4, stroke: 'none' },
				highlight: { area: { fill: 'none' } },
				xAxis: { ticks: edgeTicks, format: formatDay }
			}}
		>
			{#snippet tooltip()}
				<Chart.Tooltip labelFormatter={(d) => formatDay(d)} indicator="line" />
			{/snippet}
		</BarChart>
	</Chart.Container>
</div>
