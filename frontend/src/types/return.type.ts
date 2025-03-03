export interface FormResponse<T> {
	success: boolean;
	message: string;
	errors?: { [K in keyof T]?: string[] };
	inputs?: T;
}

export interface ReturnType {
	success: boolean;
	message: string;
}
