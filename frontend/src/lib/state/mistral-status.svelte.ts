export type MistralStatus = {
    status: 'operational' | 'down';
    downSince: string | null;
    lastChecked: string | null;
    lastError: string | null;
};

const defaultStatus: MistralStatus = { status: 'operational', downSince: null, lastChecked: null, lastError: null };

let _status = $state<MistralStatus>(defaultStatus);

export const mistralStatusState = {
    get current() {
        return _status;
    },
    set current(v: MistralStatus) {
        _status = v;
    }
}