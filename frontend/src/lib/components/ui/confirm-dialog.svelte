<script lang="ts">
    import { confirmState } from "$lib/state/confirm.svelte";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
	import Input from "./input/input.svelte";
</script>

<AlertDialog.Root open={confirmState.open}>
    <AlertDialog.Content>
        <AlertDialog.Header>
            <AlertDialog.Title>Are you sure?</AlertDialog.Title>
            <AlertDialog.Description>
                {confirmState.message}
            </AlertDialog.Description>
        </AlertDialog.Header>

        {#if confirmState.confirmWord}
            <div class="flex flex-col gap-1.5 pt-1">
                <span class="text-sm text-muted-foreground">
                    Type <span class="font-mono font-medium text-foreground">{confirmState.confirmWord}</span> to confirm
                </span>

                <Input
                    bind:value={confirmState.confirmInput}
                    placeholder={confirmState.confirmWord}
                    autofocus
                />
            </div>
        {/if}

        <AlertDialog.Footer>
            <AlertDialog.Cancel class="cursor-pointer" onclick={confirmState.cancel}>Cancel</AlertDialog.Cancel>
            <AlertDialog.Action class="cursor-pointer" onclick={confirmState.accept} disabled={!confirmState.canAccept}>Confirm</AlertDialog.Action>
        </AlertDialog.Footer>
    </AlertDialog.Content>
</AlertDialog.Root>