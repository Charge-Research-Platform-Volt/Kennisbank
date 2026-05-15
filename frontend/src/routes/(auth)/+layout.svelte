<script>
	let { children } = $props();
</script>

<div class="flex h-screen w-screen">
	<!-- Form side (left) -->
	<div class="flex w-1/2 shrink-0 flex-col items-center justify-center p-4 lg:w-2/5">
		<!-- Logo -->
		<img
			class="absolute top-4 left-4"
			src="/img/charge-logo.avif"
			width={142}
			height="56.49"
			alt="logo"
		/>

		{@render children()}
	</div>

	<!-- Image side (right) -->
	<img
		class="w-1/2 min-w-0 rounded-2xl object-cover p-2 lg:w-3/5"
		src="/img/login-image.avif"
		alt=""
	/>
</div>
