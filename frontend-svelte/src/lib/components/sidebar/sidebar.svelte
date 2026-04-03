<script lang="ts">
    import { LayoutDashboard, Search, Library, BookMarked, PanelLeftOpen, PanelLeftClose, CirclePlus, MessageCircleQuestionMark, MessageCircleMore, Wrench, Settings, LogOut } from "lucide-svelte";
    import Avatar from "$lib/components/ui/avatar/avatar.svelte";
    import { sidebar } from "$lib/state/sidebar.svelte";
    import { page } from "$app/state";
    import Button from "../ui/button/button.svelte";
    import * as DropdownMenu from "../ui/dropdown-menu";
    import { userState } from "$lib/state/user.svelte";
    import { goto } from "$app/navigation";
	import { api } from "$lib/api";

    type LucideIcon = typeof LayoutDashboard;
    
    let aside = $state<HTMLElement>();
    let collapsed = $state(!sidebar.open);
    
    $effect(() => 
    {
        if (sidebar.open) 
        {
            collapsed = false;
            return;
        }
        
        const onEnd = (e: TransitionEvent) => 
        {
            if (e.propertyName === 'width') collapsed = true;
        };
        
        aside?.addEventListener('transitionend', onEnd);
        return () => aside?.removeEventListener('transitionend', onEnd);
    });
    
    async function handleLogout() 
    {
        try { await api.post('/api/auth/logout', {}); } catch { /* Ignore error */}
        userState.user = null;
        goto('/login');
    }
</script>

{#snippet navItem(href: string, icon: LucideIcon, label: string, target?: string)}
    {@const Icon = icon}
    {@const active = href === '/' ? page.url.pathname === href : page.url.pathname.startsWith(href)}

    <a
        {href}
        target={target ?? undefined}
        class="flex items-center rounded-md py-2 text-sm transition-colors {collapsed ? 'gap-0 justify-center' : 'gap-2 px-2'}
            {active
                ? 'bg-sidebar-accent text-sidebar-accent-foreground font-medium'
                : 'text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'}"
    >
        <Icon size={18} class="shrink-0" />
        <span class="overflow-hidden whitespace-nowrap transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'}">
            {label}
        </span>
    </a>
{/snippet}

{#snippet separator(title: string)}
    <div class="flex items-center py-1 {sidebar.open ? 'gap-2' : ''}">
        <span class="overflow-hidden whitespace-nowrap text-xs font-medium text-muted-foreground transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'}">
            {title}
        </span>
        <hr class="flex-1 border-sidebar-border" />
    </div>
{/snippet}

<aside bind:this={aside} class="bg-sidebar border-sidebar-border flex h-full flex-col border-r transition-all duration-300 pt-5 gap-4 {sidebar.open ? 'w-64 px-2 pb-2' : 'w-12 px-1.5 pb-2'}">
    <!-- Header -->
    <div class="flex w-full items-center {collapsed ? 'justify-center' : 'justify-between'} px-1">
        <div class="overflow-hidden transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'} flex items-center gap-1">
            <!-- <Landmark size={20} class="shrink-0 text-muted-foreground" /> -->
            <img src="/img/charge-icon.webp" alt="Charge Icon" class="w-5 h-5" />
            <span class="whitespace-nowrap font-medium text-lg">Knowledge Bank</span>
        </div>
        <button onclick={sidebar.toggle} class="rounded-md p-1.5 text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground transition-colors">
            {#if sidebar.open}
                <PanelLeftClose size={20} />
            {:else}
                <PanelLeftOpen size={20} />
            {/if}
        </button>
    </div>

    <!-- New + Quicksearch -->
    <div class="flex flex-col w-full gap-1">
        <Button href="/new" class="transition-colors items-center {collapsed ? 'w-9 px-0 justify-center gap-0' : 'w-full justify-start px-2'}">
            <CirclePlus size={18} class="shrink-0" />
            <span class="overflow-hidden whitespace-nowrap transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'}">
                New
            </span>
        </Button>

        <Button variant="outline" class="cursor-pointer transition-colors items-center {collapsed ? 'w-9 px-0 justify-center gap-0' : 'w-full justify-start px-2'}">
            <Search size={18} class="shrink-0" />
            <span class="overflow-hidden whitespace-nowrap transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'}">
                Quicksearch
            </span>
        </Button>
    </div>
    
    <!-- Nav items -->
    <div class="flex flex-col w-full gap-1">
        {@render separator("Menu")}
        {@render navItem("/", LayoutDashboard, "Home")}
        {@render navItem("/search", Search, "Search")}
        {@render navItem("/library", Library, "Library")}
        {@render navItem("/projects", BookMarked, "Projects")}
        {@render navItem("/chatbot", MessageCircleMore, "ChatBot")}
        
        {#if userState.role === 'admin'}
            {@render navItem("/manage", Wrench, "Manage")}
        {/if}
    </div>
    
    <!-- Footer -->
    <div class="flex flex-col w-full mt-auto gap-1">
        {@render navItem('/help', MessageCircleQuestionMark, 'Help', '_blank')}
        {@render separator("")}

        <!-- User profile -->
        <DropdownMenu.Root>
            <DropdownMenu.Trigger class="cursor-pointer flex w-full items-center rounded-md py-2 text-sm transition-colors
                {collapsed ? 'gap-0 justify-center' : 'gap-2 px-2'}
                text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground">
                {#if userState.user}
                    <Avatar
                        userId={userState.user.id}
                        name="{userState.user.firstName} {userState.user.lastName}"
                        customAvatarVersion={userState.user.customAvatarVersion}
                        size={32}
                    />
                {/if}
                <div class="overflow-hidden text-left transition-all duration-300 {sidebar.open ? 'max-w-full opacity-100' : 'max-w-0 opacity-0'}">
                    <p class="whitespace-nowrap text-sm font-medium text-sidebar-foreground leading-tight">
                        {userState.user?.firstName} {userState.user?.lastName}
                    </p>
                    <p class="whitespace-nowrap text-xs text-muted-foreground leading-tight">
                        {userState.user?.email}
                    </p>
                </div>
            </DropdownMenu.Trigger>

            <DropdownMenu.Content side="top" align="start">
                <DropdownMenu.Group>
                    <DropdownMenu.Item class="cursor-pointer flex items-center gap-2" onclick={() => goto('/account')}>
                        <Settings size={10} class="text-muted-foreground shrink-0" />
                        Account Settings
                    </DropdownMenu.Item>
                    <DropdownMenu.Separator />
                    <DropdownMenu.Item class="cursor-pointer flext items-center gap-2" onclick={handleLogout}>
                        <LogOut size={10} class="text-muted-foreground shrink-0" />
                        Log Out
                    </DropdownMenu.Item>
                </DropdownMenu.Group>
            </DropdownMenu.Content>
        </DropdownMenu.Root>
    </div>
</aside>