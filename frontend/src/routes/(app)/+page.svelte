<script lang="ts">
    import { userState } from '$lib/state/user.svelte';
    import { getGreeting } from '$lib/greetings';
    import { Search, Library, BookMarked } from '@lucide/svelte';
    
    const greeting = $derived(getGreeting(userState.user?.firstName ?? ''));
</script>

<div class="flex min-h-full w-full flex-col items-center justify-center gap-6 px-6">
    <!-- Greeting -->
    <div class="flex flex-col gap-1 items-center">
        <h1 class="text-5xl font-bold">{greeting.heading}</h1>
        <p class="text-lg text-gray-500">{greeting.subtext}</p>
    </div>
    
    <!-- Buttons -->
    <div class="flex gap-3 justify-center">
        {#each [
            { icon: Search, label: 'Search', href: '/search' },
            { icon: Library, label: 'Library', href: '/library' },
            { icon: BookMarked, label: 'Projects', href: '/projects' }
        ] as btn (btn.href)}
            <a href={btn.href} class="flex h-25 w-30 flex-col items-center justify-center rounded-xl border border-gray-300 bg-gray-100 p-3 shadow-md transition hover:bg-gray-200">
                <btn.icon size={32} color="#4b5563" />
                <p class="mt-2 text-lg font-medium text-gray-600">{btn.label}</p>
            </a>
        {/each}
    </div>
</div>