<script lang="ts">
    import { api } from "$lib/api";
	import Avatar from "$lib/components/ui/avatar/avatar.svelte";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import Spinner from "$lib/components/ui/spinner/spinner.svelte";
	import * as Table from "$lib/components/ui/table";
    import { confirm } from "$lib/state/confirm.svelte";
    import type { CombinedEntry, CombinedListResponse, UserEntry } from "$lib/types/user";
    import { debounce } from "$lib/utils/debounce";
	import { Search, Plus, RefreshCw, X, Pencil, Trash2, Mail } from "@lucide/svelte";
	import * as Pagination from "$lib/components/ui/pagination";
    import { toast } from "svelte-sonner";
	import * as Dialog from "$lib/components/ui/dialog";

    const debouncedSearch = debounce(onSearch);

    let items = $state<CombinedEntry[]>([]);
    let loading = $state(true);
    let search = $state('');
    let currentPage = $state(1);
    let pageCount = $state(1);
    let totalCount = $state(0);
    let submitting = $state(false);

    // edit dialog
    let openUser = $state<UserEntry | null>(null);
    let editMail = $state('');
    let editRole = $state('');

    // invite dialog
    let inviteOpen = $state(false);
    let inviteEmail = $state('');
    let inviteRole = $state('user');

    async function fetchItems(page = currentPage) {
        loading = true;
        try {
            const result = await api.get<CombinedListResponse>(
                `/api/user/list-combined?pageIndex=${page}&pageSize=50&searchQuery=${encodeURIComponent(search)}`
            );
            items = result.body.items;
            pageCount = result.body.pageCount;
            totalCount = result.body.totalCount;
        } finally {
            loading = false;
        }
    }

    function onSearch() {
        currentPage = 1;
        pageCount = 1;
        items = [];
        fetchItems();
    }

    function openEdit(user: UserEntry) {
        openUser = user;
        editMail = user.email;
        editRole = user.role;
    }

    async function saveUser() {
        if (!openUser) return;
        submitting = true;
        try {
            if (editMail !== openUser.email)
                await api.patch('/api/user/update-mail', { userId: openUser.id, email: editMail });
            if (editRole !== openUser.role)
                await api.patch('/api/roles/assign', { userId: openUser.id, roleName: editRole });
            await fetchItems();
            openUser = null;
            toast.success('User updated.');
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to update user.');
        } finally {
            submitting = false;
        }
    }

    async function deleteUser(entry: UserEntry) {
        const ok = await confirm(`Delete ${entry.email}? This cannot be undone.`);
        if (!ok) return;
        try {
            await api.delete(`/api/user/delete?userId=${entry.id}`);
            await fetchItems();
            toast.success('User deleted.');
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to delete user.');
        }
    }

    async function inviteUser() {
        submitting = true;
        try {
            await api.post('/api/auth/invite', { email: inviteEmail, role: inviteRole });
            toast.success('Invitation sent.');
            inviteOpen = false;
            inviteEmail = '';
            inviteRole = 'user';
            await fetchItems();
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to send invitation.');
        } finally {
            submitting = false;
        }
    }

    async function resendInvite(id: string) {
        try {
            toast.loading('Sending invite...');
            await api.post(`/api/auth/resend-invite/${id}`);
            toast.success('Invitation resent.');
            await fetchItems();
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to resend invitation.');
        }
    }

    async function cancelInvite(id: string) {
        const ok = await confirm('Cancel this invitation?');
        if (!ok) return;
        try {
            await api.delete(`/api/auth/invitations/${id}`);
            await fetchItems();
            toast.success('Invitation cancelled.');
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to cancel invitation.');
        }
    }

    $effect(() => {
        fetchItems(currentPage);
    });
</script>

<div class="flex flex-1 h-full overflow-hidden">
    <!-- List -->
    <div class="flex flex-col flex-1 p-4 gap-4 overflow-hidden min-w-0">
        <!-- Header -->
        <div class="flex flex-col gap-1">
            <div class="flex items-center justify-between shrink-0 gap-5">
                <div class="flex flex-1 items-center gap-2 border border-input rounded-md bg-background px-3 focus-within:ring-2 focus-within:ring-ring/50 focus-within:border-ring shrink-0">
                    <Search size={16} class="text-muted-foreground shrink-0" />
                    <input
                        class="flex-1 py-1.5 text-sm bg-transparent outline-none placeholder:text-muted-foreground"
                        bind:value={search}
                        oninput={debouncedSearch}
                        placeholder="Search users..."
                    />
                </div>
                
                <Button class="cursor-pointer" onclick={() => inviteOpen = true}><Plus size={14} class="shrink-0" /> Invite User</Button>
            </div>
            <span class="pl-3 text-sm text-muted-foreground">{totalCount} entries</span>
        </div>

        <!-- Table -->
        <div class="flex-1 overflow-y-auto">
            {#if loading}
                <div class="flex h-full items-center justify-center">
                    <Spinner class="w-8 h-8" />
                </div>
            {:else}
                <Table.Root>
                    <Table.Header>
                        <Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
                            <Table.Head>Name / Email</Table.Head>
                            <Table.Head class="w-px">Role</Table.Head>
                            <Table.Head class="w-px"></Table.Head>
                        </Table.Row>
                    </Table.Header>

                    <Table.Body>
                        {#if items.length === 0}
                            <Table.Row>
                                <Table.Cell colspan={3} class="py-12 text-center text-sm text-muted-foreground">
                                    No users found.
                                </Table.Cell>
                            </Table.Row>
                        {/if}

                        {#each items as entry (entry.id)}
                            <Table.Row class="group cursor-pointer" onclick={() => { if (entry.type === 'user') openEdit(entry) }}>
                                <!-- Name / Email cell -->
                                <Table.Cell class="py-3">
                                    {#if entry.type === 'user'}
                                        <div class="flex gap-4">
                                            <Avatar userId={entry.id} name="{entry.firstName} {entry.lastName}" customAvatarVersion={entry.customAvatarVersion} size={30} />

                                            <div class="flex flex-col">
                                                <span class="text-sm font-medium">{entry.firstName} {entry.lastName}</span>
                                                <span class="text-xs text-muted-foreground">{entry.email}</span>
                                            </div>
                                        </div>
                                    {:else}
                                        <div class="flex gap-4">
                                            <div class="flex items-center justify-center shrink-0" style="width: 30px; height: 30px;">
                                                <Mail size={16} class="text-muted-foreground" />
                                            </div>

                                            <div class="flex flex-col justify-center">
                                                <span class="text-sm text-muted-foreground">{entry.email}</span>
                                            </div>
                                        </div>
                                    {/if}
                                </Table.Cell>

                                <!-- Role badge cell -->
                                <Table.Cell class="py-3">
                                    <span class="text-xs rounded-full px-2 py-0.5 whitespace-nowrap w-full inline-block text-center
                                        {entry.type === 'invited' 
                                            ? 'bg-muted text-muted-foreground'
                                            : entry.role === 'admin'
                                                ? 'bg-primary/10 text-primary'
                                                : 'bg-muted text-muted-foreground'}">
                                        {entry.type === 'invited' ? 'Pending' : entry.role}            
                                    </span>
                                </Table.Cell>

                                <!-- Actions cell -->
                                <Table.Cell class="py-3 text-right">
                                    <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                        {#if entry.type === 'invited'}
                                            <button onclick={(e) => { e.stopPropagation(); resendInvite(entry.id); }} class="p-1.5 rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer">
                                                <RefreshCw size={14} class="shrink-0" />
                                            </button>
                                            <button onclick={(e) => {e.stopPropagation(); cancelInvite(entry.id); }} class="p-1.5 rounded-md text-muted-foreground hover:bg-accent hover:text-destructive transition-colors cursor-pointer">
                                                <X size={14} class="shrink-0" />
                                            </button>
                                        {:else}
                                            <button onclick={(e) => {e.stopPropagation(); openEdit(entry); }} class="p-1.5 rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer">
                                                <Pencil size={14} class="shrink-0" />
                                            </button>
                                            <button onclick={(e) => {e.stopPropagation(); deleteUser(entry); }} class="p-1.5 rounded-md text-muted-foreground hover:bg-accent hover:text-destructive transition-colors cursor-pointer">
                                                <Trash2 size={14} class="shrink-0" />
                                            </button>
                                        {/if}
                                    </div>
                                </Table.Cell>
                            </Table.Row>
                        {/each}
                    </Table.Body>
                </Table.Root>
            {/if}
        </div>

        {#if pageCount > 1}
            <Pagination.Root bind:page={currentPage} count={totalCount} perPage={50} class="shrink-0">
                {#snippet children({ pages, currentPage: cp })}
                    <Pagination.Content>
                        <Pagination.Item><Pagination.PrevButton /></Pagination.Item>
                        {#each pages as page (page.key)}
                            {#if page.type === 'ellipsis'}
                                <Pagination.Item><Pagination.Ellipsis /></Pagination.Item>
                            {:else}
                                <Pagination.Item>
                                    <Pagination.Link {page} isActive={cp === page.value} />
                                </Pagination.Item>
                            {/if}
                        {/each}
                        <Pagination.Item><Pagination.NextButton /></Pagination.Item>
                    </Pagination.Content>
                {/snippet}
            </Pagination.Root>
        {/if}
    </div>

    <!-- User panel -->
    <aside class="bg-background flex flex-col shrink-0 overflow-hidden transition-all duration-300 {openUser ? 'w-96 border-l' : 'w-0'}">
        <!-- Header -->
        <div class="flex items-start justify-between p-4 border-b">
            <div class="flex gap-4">
                {#if openUser}
                    <Avatar userId={openUser?.id} name="{openUser?.firstName} {openUser?.lastName}" customAvatarVersion={openUser?.customAvatarVersion} size={40} />
                {/if}

                <div class="flex flex-col gap-0.5">
                    <span class="font-semibold">{openUser?.firstName} {openUser?.lastName}</span>
                    <span class="text-xs text-muted-foreground">{openUser?.email}</span>
                </div>
            </div>
            
            <button class="cursor-pointer" onclick={() => openUser = null}>
                <X class="text-muted-foreground shrink-0" size={16} />
            </button>
        </div>

        <!-- Content -->
        <div class="flex flex-col flex-1 p-4 gap-4 overflow-y-auto">
            <div class="flex flex-col gap-1.5">
                <label class="text-sm font-medium" for="edit-email">Email</label>
                <Input id="edit-email" type="email" bind:value={editMail} />
            </div>

            <div class="flex flex-col gap-1.5">
                <label class="text-sm font-medium" for="edit-role">Role</label>
                <select
                    id="edit-role"
                    class="border-input bg-background text-sm flex h-9 w-full rounded-md border px-3 py-1 shadow-xs outline-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] cursor-pointer"
                    bind:value={editRole}
                >
                    <option value="user">User</option>
                    <option value="admin">Admin</option>
                </select>
            </div>
        </div>

        <!-- Footer -->
        <div class="flex justify-end gap-2 p-4 border-t shrink-0">
            <Button class="cursor-pointer" variant="ghost" onclick={() => openUser = null}>
                Cancel
            </Button>

            <Button class="cursor-pointer" onclick={saveUser} disabled={submitting}>
                {#if submitting}<Spinner class="w-4 h-4" />{/if} Save
            </Button>
        </div>
    </aside>

    <Dialog.Root bind:open={inviteOpen}>
        <Dialog.Content onkeydown={(e) => { if (e.key === 'Enter') inviteUser(); }}>
            <Dialog.Header>
                <Dialog.Title>Invite User</Dialog.Title>
                <Dialog.Description>An invitation email will be sent to the address below.</Dialog.Description>
            </Dialog.Header>

            <div class="flex flex-col gap-1.5">
                <label class="text-sm font-medium" for="invite-email">Email</label>
                <Input id="invite-email" type="email" bind:value={inviteEmail} placeholder="user@example.com" />
            </div>

            <div class="flex flex-col gap-1.5">
                <label class="text-sm font-medium" for="invite-role">Role</label>
                <select
                    id="invite-role"
                    class="border-input bg-background text-sm flex h-9 w-full rounded-md border px-3 py-1 shadow-xs outline-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] cursor-pointer"
                    bind:value={inviteRole}
                >
                    <option value="user">User</option>
                    <option value="admin">Admin</option>
                </select>
            </div>

            <Dialog.Footer>
                <Button class="cursor-pointer" variant="ghost" onclick={() => inviteOpen = false}>Cancel</Button>
                <Button class="cursor-pointer" onclick={inviteUser} disabled={submitting}>
                    {#if submitting}<Spinner class="w-4 h-4" />{/if} Send Invite
                </Button>
            </Dialog.Footer>
        </Dialog.Content>
    </Dialog.Root>
</div>