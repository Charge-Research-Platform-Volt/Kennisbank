import { afterEach, describe, expect, it, vi } from 'vitest';
import { File, FileText, Globe, Music, Video, User, Building2 } from '@lucide/svelte';
import { debounce } from './debounce';
import { getFileAction, getFileIcon } from './icons';
import { formatLanguage } from './locale';
import { isPasswordValid } from './password';

describe('isPasswordValid', () => {
	it.each([
		['Abc12!', true], // exactly the minimum length
		['Sterk-Wachtwoord1', true],
		['Ab1!x', false], // one character too short
		['abcdef1!', false], // no uppercase
		['ABCDEF1!', false], // no lowercase
		['Abcdefg!', false], // no digit
		['Abcdefg1', false], // no special character
		['', false]
	])('%j → %s', (password, expected) => {
		expect(isPasswordValid(password)).toBe(expected);
	});
});

describe('formatLanguage', () => {
	it('turns a language code into its English name', () => {
		expect(formatLanguage('nl')).toBe('Dutch');
		expect(formatLanguage('en')).toBe('English');
	});

	it('falls back to the code when it is not a valid language tag', () => {
		expect(formatLanguage('not a code')).toBe('not a code');
	});
});

describe('getFileAction', () => {
	it.each([
		['website', 'open'],
		['document', 'download'],
		['PDF', 'download'],
		['mp3', 'download'],
		['video', 'download'],
		['person', null],
		['organisation', null],
		['unknown', null]
	])('%s → %s', (fileType, expected) => {
		expect(getFileAction(fileType)).toBe(expected);
	});
});

describe('getFileIcon', () => {
	it.each([
		['document', FileText],
		['DOCX', FileText],
		['wav', Music],
		['mov', Video],
		['website', Globe],
		['person', User],
		['organisation', Building2],
		['something-else', File]
	])('%s', (fileType, expected) => {
		expect(getFileIcon(fileType)).toBe(expected);
	});
});

describe('debounce', () => {
	afterEach(() => {
		vi.useRealTimers();
	});

	it('only calls once, with the last arguments, after the delay', () => {
		vi.useFakeTimers();
		const fn = vi.fn();
		const debounced = debounce(fn, 200);

		debounced('s');
		debounced('so');
		debounced('sol');
		vi.advanceTimersByTime(199);
		expect(fn).not.toHaveBeenCalled();

		vi.advanceTimersByTime(1);
		expect(fn).toHaveBeenCalledOnce();
		expect(fn).toHaveBeenCalledWith('sol');
	});

	it('calls again for a new burst after the delay', () => {
		vi.useFakeTimers();
		const fn = vi.fn();
		const debounced = debounce(fn);

		debounced('a');
		vi.advanceTimersByTime(300);
		debounced('b');
		vi.advanceTimersByTime(300);

		expect(fn.mock.calls).toEqual([['a'], ['b']]);
	});
});
