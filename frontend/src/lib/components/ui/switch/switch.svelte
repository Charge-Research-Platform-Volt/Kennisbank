<script lang="ts">
	let {
		ref = $bindable<HTMLButtonElement | null>(null),
		checked = $bindable(false),
		id,
		disabled = false,
		class: className,
		...restProps
	}: {
		ref?: HTMLButtonElement | null;
		checked?: boolean;
		id?: string;
		disabled?: boolean;
		class?: string;
		[key: string]: unknown;
	} = $props();
</script>

<button
	bind:this={ref}
	{id}
	{disabled}
	role="switch"
	aria-checked={checked}
	onclick={() => {
		if (!disabled) checked = !checked;
	}}
	class="relative inline-flex h-[18px] w-8 shrink-0 cursor-pointer rounded-full border border-transparent p-0 transition-colors outline-none disabled:cursor-not-allowed disabled:opacity-50 {checked
		? 'bg-primary'
		: 'bg-input'} {className ?? ''}"
	{...restProps}
>
	<span
		class="absolute top-0 bottom-0 my-auto size-4 rounded-full bg-background shadow-sm transition-transform {checked
			? 'translate-x-[14px]'
			: 'translate-x-0'}"
	></span>
</button>
