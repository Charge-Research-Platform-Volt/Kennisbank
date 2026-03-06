<script lang="ts">
    import type { User } from '$lib/state/user.svelte';
    import { userState } from '$lib/state/user.svelte';
    import { api } from '$lib/api';
    import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import Sidebar from '$lib/components/sidebar/sidebar.svelte';
    
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
        <Spinner size={32} />
    </div>
{:else}
    <div class="flex h-screen w-full">
        <!-- Sidebar -->
        <Sidebar />

        <!-- Page content -->
        <main class="flex-1 overflow-y-auto">
            {@render children()}
        </main>
    </div>
{/if}