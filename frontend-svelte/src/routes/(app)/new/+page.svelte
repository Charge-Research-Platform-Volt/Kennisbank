<script lang="ts">
    import { FileText, User, Building2, type Icon } from 'lucide-svelte';
</script>

{#snippet card(href: string, icon: typeof Icon, title: string, subtext: string)}
    {@const LucideIcon = icon}
    
    <a {href} class="border-border hover:bg-accent rounded-lg border p-6 transition-colors">
        <LucideIcon class="text-muted-foreground mb-4 h-8 w-8" />
        <h2 class="font-medium">{title}</h2>
        <p class="text-muted-foreground mt-1 text-sm">{subtext}</p>
    </a>
{/snippet}

<div class="flex flex-col gap-8 p-8 mx-auto max-w-2xl h-full justify-center">
    <div>
        <h1 class="text-2xl font-semibold">Add New</h1>
        <p class="text-muted-foreground mt-1 text-sm">Choose what you want to add to the PowerGrid.</p>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {@render card('/new/resource', FileText, 'Resource', 'Add a document or website to the library')}
        {@render card('/new/person', User, 'Person', 'Add a person to the library')}
        {@render card('/new/organisation', Building2, 'Organisation', 'Add an organisation to the library')}
    </div>
</div>