let tick = $state(0);

export const chatRefresh = {
	get tick() {
		return tick;
	},
	trigger() {
		tick++;
	}
};
