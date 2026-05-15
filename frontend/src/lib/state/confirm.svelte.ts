let open = $state(false);
let message = $state('');
let confirmWord = $state<string | null>(null);
let confirmInput = $state('');
let resolve: (value: boolean) => void;

export function confirm(msg: string, requireWord?: string): Promise<boolean> {
	message = msg;
	confirmWord = requireWord ?? null;
	confirmInput = '';
	open = true;

	return new Promise((r) => {
		resolve = r;
	});
}

export const confirmState = {
	get open() {
		return open;
	},
	get message() {
		return message;
	},
	get confirmWord() {
		return confirmWord;
	},
	get confirmInput() {
		return confirmInput;
	},
	set confirmInput(v: string) {
		confirmInput = v;
	},
	get canAccept() {
		return confirmWord === null || confirmInput === confirmWord;
	},
	accept() {
		open = false;
		resolve(true);
	},
	cancel() {
		open = false;
		resolve(false);
	}
};
