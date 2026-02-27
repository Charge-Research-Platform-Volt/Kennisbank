<script lang="ts">
    import type { User } from '$lib/state/user.svelte';
    import { userState } from '$lib/state/user.svelte';
    import { api } from '$lib/api';
	import { Loader } from 'lucide-svelte';
    
    let { children } = $props();
    
    // Fetch user role, which also automatically checks authentication
    api.get<User>('/api/user/current/account').then(res =>
    {
        userState.user = res.body;
        userState.loading = false;
    }).catch(() =>
    {
        userState.loading = false;
    });
</script>

{#if userState.loading}
    <div class="flex h-screen w-full items-center justify-center">
        <Loader class="animate-spin text-gray-400" size={32} />
    </div>
{:else}
    <div class="flex h-screen w-full">
        <!-- Sidebar -->

        <!-- Page content -->
        <main class="flex-1 overflow-y-auto">
            {@render children()}
        </main>
    </div>
{/if}