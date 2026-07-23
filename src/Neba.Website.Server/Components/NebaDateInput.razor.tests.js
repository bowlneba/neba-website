import { describe, test, expect, beforeEach, jest } from '@jest/globals';
import { initialize, setValue, dispose } from './NebaDateInput.razor.js';

function makeContainer(id) {
    const container = document.createElement('div');
    container.id = id;
    container.innerHTML = `
        <input data-segment="month" />
        <input data-segment="day" />
        <input data-segment="year" />
    `;
    document.body.appendChild(container);
    return container;
}

function makeDotNetRef() {
    return { invokeMethodAsync: jest.fn() };
}

function segments(containerId) {
    const container = document.getElementById(containerId);
    return {
        month: container.querySelector('[data-segment="month"]'),
        day: container.querySelector('[data-segment="day"]'),
        year: container.querySelector('[data-segment="year"]')
    };
}

function typeDigits(input, digits) {
    input.value = (input.value + digits).slice(0, input.value.length + digits.length);
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function pressKey(input, key) {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    input.dispatchEvent(event);
    return event;
}

describe('NebaDateInput', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
    });

    describe('initialize', () => {
        test('should seed the three segments from the initial values', () => {
            makeContainer('date-1');
            initialize('date-1', makeDotNetRef(), '08', '05', '2026');

            const { month, day, year } = segments('date-1');
            expect(month.value).toBe('08');
            expect(day.value).toBe('05');
            expect(year.value).toBe('2026');
        });
    });

    describe('auto-advance on digit fill', () => {
        test('should move focus from month to day once two digits are entered', () => {
            makeContainer('date-2');
            initialize('date-2', makeDotNetRef(), '', '', '');
            const { month, day } = segments('date-2');

            typeDigits(month, '08');

            expect(document.activeElement).toBe(day);
        });

        test('should move focus from day to year once two digits are entered', () => {
            makeContainer('date-3');
            initialize('date-3', makeDotNetRef(), '', '', '');
            const { day, year } = segments('date-3');

            typeDigits(day, '05');

            expect(document.activeElement).toBe(year);
        });

        test('should not advance out of year (last segment) when it fills', () => {
            makeContainer('date-4');
            initialize('date-4', makeDotNetRef(), '', '', '');
            const { year } = segments('date-4');

            year.focus();
            typeDigits(year, '2026');

            expect(document.activeElement).toBe(year);
        });

        test('should strip non-digit characters typed into a segment', () => {
            makeContainer('date-5');
            initialize('date-5', makeDotNetRef(), '', '', '');
            const { month } = segments('date-5');

            month.value = 'a8';
            month.dispatchEvent(new Event('input', { bubbles: true }));

            expect(month.value).toBe('8');
        });
    });

    describe('"/" key navigation — the reported bug', () => {
        test('should advance from month to day when "/" is pressed after two digits', () => {
            makeContainer('date-6');
            initialize('date-6', makeDotNetRef(), '', '', '');
            const { month, day } = segments('date-6');

            typeDigits(month, '08');
            pressKey(month, '/');

            expect(document.activeElement).toBe(day);
        });

        test('should not double-advance past day when "/" is pressed immediately after auto-advancing from month', () => {
            // Reproduces the exact reported bug: typing "08/05/2026" — the "/" right after "08" must
            // land on (and stay on) the now-focused, still-empty day segment, not skip it into year.
            makeContainer('date-7');
            initialize('date-7', makeDotNetRef(), '', '', '');
            const { month, day, year } = segments('date-7');

            typeDigits(month, '08'); // auto-advances focus to day
            pressKey(day, '/'); // day is empty — must be a no-op, not skip to year

            expect(document.activeElement).toBe(day);
            expect(year.value).toBe('');
        });

        test('should advance from a single (unambiguous) digit day when "/" is pressed', () => {
            makeContainer('date-8');
            initialize('date-8', makeDotNetRef(), '', '', '');
            const { day, year } = segments('date-8');

            day.focus();
            typeDigits(day, '5');
            pressKey(day, '/');

            expect(document.activeElement).toBe(year);
        });

        test('should not insert a literal "/" character into the segment value', () => {
            makeContainer('date-9');
            initialize('date-9', makeDotNetRef(), '', '', '');
            const { month } = segments('date-9');

            month.focus();
            typeDigits(month, '9');
            const event = pressKey(month, '/');

            expect(event.defaultPrevented).toBe(true);
            expect(month.value).toBe('9');
        });

        test('should end-to-end resolve "08/05/2026" typed with slashes to clean segment values', () => {
            makeContainer('date-10');
            initialize('date-10', makeDotNetRef(), '', '', '');
            const { month, day, year } = segments('date-10');

            typeDigits(month, '0');
            typeDigits(month, '8'); // auto-advance to day
            pressKey(day, '/'); // no-op, day still empty
            typeDigits(day, '0');
            typeDigits(day, '5'); // auto-advance to year
            pressKey(year, '/'); // no-op, last segment
            typeDigits(year, '2');
            typeDigits(year, '0');
            typeDigits(year, '2');
            typeDigits(year, '6');

            expect(month.value).toBe('08');
            expect(day.value).toBe('05');
            expect(year.value).toBe('2026');
        });
    });

    describe('backspace navigation', () => {
        test('should move focus back to month when backspace is pressed on an empty day segment', () => {
            makeContainer('date-11');
            initialize('date-11', makeDotNetRef(), '08', '', '');
            const { month, day } = segments('date-11');

            pressKey(day, 'Backspace');

            expect(document.activeElement).toBe(month);
        });

        test('should not move focus when backspace is pressed on a non-empty segment', () => {
            makeContainer('date-12');
            initialize('date-12', makeDotNetRef(), '', '', '');
            const { month, day } = segments('date-12');

            day.focus();
            typeDigits(day, '5');
            pressKey(day, 'Backspace');

            expect(document.activeElement).toBe(day);
        });
    });

    describe('non-digit key filtering', () => {
        test('should prevent default for a letter keydown', () => {
            makeContainer('date-13');
            initialize('date-13', makeDotNetRef(), '', '', '');
            const { month } = segments('date-13');

            const event = pressKey(month, 'a');

            expect(event.defaultPrevented).toBe(true);
        });

        test('should allow a digit keydown through', () => {
            makeContainer('date-14');
            initialize('date-14', makeDotNetRef(), '', '', '');
            const { month } = segments('date-14');

            const event = pressKey(month, '5');

            expect(event.defaultPrevented).toBe(false);
        });

        test('should not prevent default for a modified key combo (e.g. Ctrl+V paste)', () => {
            makeContainer('date-15');
            initialize('date-15', makeDotNetRef(), '', '', '');
            const { month } = segments('date-15');

            const event = new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true, cancelable: true });
            month.dispatchEvent(event);

            expect(event.defaultPrevented).toBe(false);
        });
    });

    describe('.NET notification', () => {
        test('should notify .NET with the composed segment values on every input', () => {
            makeContainer('date-16');
            const dotNetRef = makeDotNetRef();
            initialize('date-16', dotNetRef, '', '', '');
            const { month } = segments('date-16');

            typeDigits(month, '08');

            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifySegmentsChanged', '08', '', '');
        });
    });

    describe('setValue', () => {
        test('should overwrite all three segments for a known container', () => {
            makeContainer('date-17');
            initialize('date-17', makeDotNetRef(), '', '', '');

            setValue('date-17', '01', '15', '2027');

            const { month, day, year } = segments('date-17');
            expect(month.value).toBe('01');
            expect(day.value).toBe('15');
            expect(year.value).toBe('2027');
        });

        test('should silently no-op for an unknown container id', () => {
            expect(() => setValue('does-not-exist', '01', '15', '2027')).not.toThrow();
        });
    });

    describe('dispose', () => {
        test('should remove event listeners so further input no longer notifies .NET', () => {
            makeContainer('date-18');
            const dotNetRef = makeDotNetRef();
            initialize('date-18', dotNetRef, '', '', '');
            const { month } = segments('date-18');

            dispose('date-18');
            typeDigits(month, '08');

            expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('should silently no-op for an unknown container id', () => {
            expect(() => dispose('does-not-exist')).not.toThrow();
        });
    });
});
