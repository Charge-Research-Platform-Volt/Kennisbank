<script lang="ts">
    import Input from "$lib/components/ui/input/input.svelte";
    import Button from "$lib/components/ui/button/button.svelte";
	import { goto } from "$app/navigation";
	import { page } from "$app/state";

    const redirect = page.url.searchParams.get('redirect');

    let email = $state('');
    let password = $state('');
    let loading = $state(false);
    let error = $state('');
    
    async function handleSubmit(e: SubmitEvent) 
    {
        // Prevent default page reload
        e.preventDefault();
    
        loading = true;
        error = '';
    
        try 
        {
            // Log in on backend
            const response = await fetch('/api/auth/login?useCookies=true&useSessionCookies=true', 
            {
                method: 'POST',
                headers: { 'Content-Type': "application/json" },
                body: JSON.stringify({ email, password })
            });
            
            if (response.ok) 
            {
                goto(redirect ?? '/');
                return;
            }
            
            password = '';
            error = "Invalid credentials";
        }
        catch 
        {
            error = 'Something went wrong, please try again';
        }
        finally 
        {
            loading = false;
        }
    }
</script>

<!-- Form -->
<div class="mx-auto w-3/4 p-4">
    <form class="flex flex-col gap-4" onsubmit={handleSubmit}>
        
        <div class="flex flex-col gap-1">
            <h1 class="text-3xl font-bold">Sign in</h1>
            <p>Welcome back!</p>
        </div>
        
        <div class="flex flex-col gap-1">
            <label for="email" class="font-bold">Email</label>
            <Input bind:value={email} type="email" name="email" id="email" required />
        </div>
        
        <div class="flex flex-col gap-1">
            <label for="password" class="font-bold">Password</label>
            <Input bind:value={password} type="password" name="password" id="password" required />
        </div>
        
        <Button type="submit" class="w-full" disabled={!email || !password || loading}>
            {#if loading}
                Signing in...
            {:else}
                Sign in
            {/if}
        </Button>
        
        {#if error}
            <p class="pl-2 text-sm text-destructive">{error}</p>
        {/if}
    </form>
</div>