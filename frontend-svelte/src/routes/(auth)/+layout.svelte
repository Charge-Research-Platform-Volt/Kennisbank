<script>
    let { children } = $props();
</script>

<div class="flex w-screen h-screen">
    <!-- Form side (left) -->
    <div class="w-1/2 lg:w-2/5 shrink-0 flex flex-col justify-center items-center p-4">
        <!-- Logo -->
        <img class="absolute top-4 left-4" src="/img/charge-logo.avif" width={142} height="56.49" alt="logo" />

        {@render children()}
    </div>

    <!-- Image side (right) -->
    <img class="w-1/2 lg:w-3/5 min-w-0 p-2 rounded-2xl object-cover" src="/img/login-image.avif" alt="" />
</div>