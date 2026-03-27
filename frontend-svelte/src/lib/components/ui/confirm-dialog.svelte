<script lang="ts">
    import { confirmState } from "$lib/state/confirm.svelte";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
</script>

<AlertDialog.Root open={confirmState.open}>
    <AlertDialog.Content>
        <AlertDialog.Header>
            <AlertDialog.Title>Are you sure?</AlertDialog.Title>
            <AlertDialog.Description>
                {confirmState.message}
            </AlertDialog.Description>
        </AlertDialog.Header>

        <AlertDialog.Footer>
            <AlertDialog.Cancel class="cursor-pointer" onclick={confirmState.cancel}>Cancel</AlertDialog.Cancel>
            <AlertDialog.Action class="cursor-pointer" onclick={confirmState.accept}>Confirm</AlertDialog.Action>
        </AlertDialog.Footer>
    </AlertDialog.Content>
</AlertDialog.Root>