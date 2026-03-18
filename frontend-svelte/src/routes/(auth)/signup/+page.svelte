<script lang="ts">
    import Input from "$lib/components/ui/input/input.svelte";
    import Button from "$lib/components/ui/button/button.svelte";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
	import { api } from "$lib/api";
	import { Pencil } from "lucide-svelte";
    import Avatar from "$lib/components/ui/avatar/avatar.svelte";
    
    const token = page.url.searchParams.get('token');
    
    let firstName = $state('');
    let lastName = $state('');
    let email = $state('');
    let password = $state('');
    let passwordRepeat = $state('');
    
    let avatarInputElement: HTMLInputElement | null = $state(null);
    let avatarFile: File | null = $state(null);
    let avatarPreviewUrl = $derived(avatarFile ? URL.createObjectURL(avatarFile) : null);
    
    let loading = $state(false);
    let error = $state();
    
    async function handleSubmit(e: SubmitEvent) 
    {
        // Prevent default page reload
        e.preventDefault();
        
        if (password != passwordRepeat) 
        {
            error = 'Passwords don\'t match';
            return;
        }
        
        loading = true;
        error = '';
        
        try 
        {
            // Sign up on backend
            const formData = new FormData();
            
            formData.append('firstName', firstName);
            formData.append('lastName', lastName);
            formData.append('email', email);
            formData.append('password', password);
            formData.append('token', token ?? '');
            
            if (avatarFile)
                formData.append('avatar', avatarFile);
                
            const signupResponse = await api.form('/api/auth/signup', formData);
            
            if (!signupResponse.success) 
            {
                error = signupResponse.message;
                return;
            }
            
            // Log in on backend
            const loginResponse = await fetch('/api/auth/login?useCookies=true&useSessionCookies=true', 
            {
                method: 'POST',
                headers: { 'Content-Type': "application/json" },
                body: JSON.stringify({ email, password })
            });
            
            goto(loginResponse.ok ? '/' : '/login');
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

<div class="mx-auto w-3/4 p-4">
    <form class="flex flex-col gap-4" onsubmit={handleSubmit}>
    
        <div class="flex flex-col gap-1">
            <h1 class="text-3xl font-bold">Sign up</h1>
            <p>Welcome! We're happy to have you.</p>
        </div>
        
        <!-- Avatar + Name -->
        <div class="flex gap-10">
            <!-- Avatar -->
            <div class="flex w-1/3 flex-col gap-1 justify-center items-center">
                <button type="button" onclick={() => avatarInputElement?.click()} class="m-0 p-0 bg-transparent border-none group relative cursor-pointer">
                    <Avatar
                        name="{firstName} {lastName}"
                        src={avatarPreviewUrl}
                        size={140}
                    />
                    <div class="absolute inset-0 flex items-center justify-center rounded-full opacity-0 group-hover:opacity-100 transition-opacity bg-black/30">
                        <Pencil class="text-white" size={20} />
                    </div>
                </button>
                <input
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    bind:this={avatarInputElement}
                    onchange={(e) => (avatarFile = e.currentTarget.files?.[0] ?? null)}
                    hidden
                />
            </div>
            
            <!-- Name fields -->
            <div class="flex flex-1 flex-col gap-1">
                <div class="flex flex-1 flex-col gap-1">
                    <label for="firstName" class="font-bold">First Name</label>
                    <Input bind:value={firstName} type="text" id="firstName" required />
                </div>
                
                <div class="flex flex-1 flex-col gap-1">
                    <label for="lastName" class="font-bold">Last Name</label>
                    <Input bind:value={lastName} type="text" id="lastName" required />
                </div>
            </div>
        </div>
        
        <!-- Email -->
        <div class="flex flex-col gap-1">
            <label for="email" class="font-bold">Email</label>
            <Input bind:value={email} type="email" name="email" id="email" required />
        </div>
        
        <!-- Password -->
        <div class="flex flex-col gap-1">
            <label for="password" class="font-bold">Password</label>
            <Input bind:value={password} type="password" name="password" id="password" required />
        </div>
        
        <div class="flex flex-col gap-1">
            <label for="passwordRepeat" class="font-bold">Repeat Password</label>
            <Input bind:value={passwordRepeat} type="password" name="passwordRepeat" id="passwordRepeat" required />
        </div>
        
        <Button type="submit" class="w-full" disabled={!firstName || !lastName || !email || !password || !passwordRepeat || loading}>
            {#if loading}
                Signing up...
            {:else}
                Sign up
            {/if}
        </Button>
        
        {#if error}
            <p class="pl-2 text-sm text-destructive">{error}</p>
        {/if}
        
        <p class="text-center text-muted-foreground">Already have an account? <a href='/login' class="text-blue-500">Log in</a>.</p>
    
    </form>
</div>