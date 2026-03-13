<script lang="ts">
    import { getContext, untrack } from 'svelte';
    import type { DatePrecision } from '$lib/types/resource';
    import { formatDate } from '$lib/utils/date';

    let { date, precision, onsave }: {
        date?: string;
        precision?: DatePrecision;
        onsave: (date: string | null, precision: DatePrecision) => Promise<void>;
    } = $props();

    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());
    const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

    let localPrecision = $state<DatePrecision>(untrack(() => precision ?? 'Day'));
    let inputState = $state(untrack(() => getInputStr()));

    function getInputStr(): string {
        if (!date) return '';
        const d = new Date(date);
        if (localPrecision === 'Year') return String(d.getFullYear());
        if (localPrecision === 'Month') return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
        return date.split('T')[0];
    }

    function toIsoDate(val: string, prec: DatePrecision): string | null {
        if (!val) return null;
        if (prec === 'Year') return `${val}-01-01`;
        if (prec === 'Month') return `${val}-01`;
        return val;
    }

    function setPrecision(p: DatePrecision) {
        localPrecision = p;
        inputState = getInputStr();
        registerSave?.(onsave(toIsoDate(inputState, p), p));
    }

    function handleBlur() {
        registerSave?.(onsave(toIsoDate(inputState, localPrecision), localPrecision));
    }
</script>

{#if editMode}
    <div class="flex flex-col gap-1.5">
        <div class="flex items-center gap-1">
            <span class="mr-1">Published</span>
            {#each (['Year', 'Month', 'Day'] as DatePrecision[]) as p}
                <button
                    onclick={() => setPrecision(p)}
                    class="px-1.5 py-0.5 rounded {localPrecision === p ? 'bg-primary text-primary-foreground' : 'bg-muted hover:bg-muted/80'}"
                >
                    {p}
                </button>
            {/each}
            {#if inputState}
                <button
                    onclick={() => { inputState = ''; registerSave?.(onsave(null, localPrecision)); }}
                    class="px-1.5 py-0.5 rounded text-destructive hover:bg-destructive/10 ml-auto"
                >
                    Clear
                </button>
            {/if}
        </div>
        {#if localPrecision === 'Year'}
            <input
                type="number"
                bind:value={inputState}
                onblur={handleBlur}
                min="1900" max="2100"
                placeholder="YYYY"
                class="bg-transparent border-b border-input focus:outline-none focus:border-ring py-0.5 w-20"
            />
        {:else if localPrecision === 'Month'}
            <div class="flex gap-2">
                <select
                    value={inputState.split('-')[1] ?? '01'}
                    onchange={(e) => {
                        const year = inputState.split('-')[0] || String(new Date().getFullYear());
                        inputState = `${year}-${(e.target as HTMLSelectElement).value}`;
                        handleBlur();
                    }}
                    class="bg-transparent border-b border-input focus:outline-none focus:border-ring py-0.5"
                >
                    <option value="">Month</option>
                    {#each [['01','January'],['02','February'],['03','March'],['04','April'],['05','May'],['06','June'],['07','July'],['08','August'],['09','September'],['10','October'],['11','November'],['12','December']] as [val, label]}
                        <option value={val}>{label}</option>
                    {/each}
                </select>
                <input
                    type="number"
                    value={inputState.split('-')[0] ?? ''}
                    onblur={(e) => {
                        const month = inputState.split('-')[1] || '01';
                        inputState = `${(e.target as HTMLInputElement).value}-${month}`;
                        handleBlur();
                    }}
                    min="1900" max="2100"
                    placeholder="YYYY"
                    class="bg-transparent border-b border-input focus:outline-none focus:border-ring py-0.5 w-16"
                />
            </div>
        {:else}
            <input
                type="date"
                bind:value={inputState}
                onblur={handleBlur}
                class="bg-transparent border-b border-input focus:outline-none focus:border-ring py-0.5"
            />
        {/if}
    </div>
{:else}
    <span>Published: {formatDate(date ?? '', precision) ?? '-'}</span>
{/if}
