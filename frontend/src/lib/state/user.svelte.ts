export type User = {
	id: string;
	firstName: string;
	lastName: string;
	email: string;
	role: string;
	customAvatarVersion: number | null;
} | null;

let _user = $state<User>(null);
let _loading = $state(true);

export const userState = {
	get user() {
		return _user;
	},
	get role() {
		return _user?.role ?? null;
	},
	get loading() {
		return _loading;
	},

	set user(v: User) {
		_user = v;
	},
	set loading(v: boolean) {
		_loading = v;
	}
};
