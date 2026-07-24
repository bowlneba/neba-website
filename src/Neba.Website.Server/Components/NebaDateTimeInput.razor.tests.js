import { describe, test, expect, beforeEach, jest } from '@jest/globals';
import { initialize, setValue, dispose } from './NebaDateTimeInput.razor.js';

function makeContainer(id) {
    const container = document.createElement('div');
    container.id = id;
    container.innerHTML = `
        <input data-segment="month" />
        <input data-segment="day" />
        <input data-segment="year" />
        <input data-segment="hour" />
        <input data-segment="minute" />
        <input data-segment="meridiem" />
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
        year: container.querySelector('[data-segment="year"]'),
        hour: container.querySelector('[data-segment="hour"]'),
        minute: container.querySelector('[data-segment="minute"]'),
        meridiem: container.querySelector('[data-segment="meridiem"]')
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

describe('NebaDateTimeInput', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
    });

    describe('initialize', () => {
        test('should seed all six segments from the initial values', () => {
            makeContainer('dt-1');
            initialize('dt-1', makeDotNetRef(), '09', '01', '2026', '09', '00', 'AM');

            const s = segments('dt-1');
            expect(s.month.value).toBe('09');
            expect(s.day.value).toBe('01');
            expect(s.year.value).toBe('2026');
            expect(s.hour.value).toBe('09');
            expect(s.minute.value).toBe('00');
            expect(s.meridiem.value).toBe('AM');
        });
    });

    describe('auto-advance on digit fill', () => {
        test('should move focus from hour to minute once two digits are entered', () => {
            makeContainer('dt-2');
            initialize('dt-2', makeDotNetRef(), '09', '01', '2026', '', '', '');
            const { hour, minute } = segments('dt-2');

            hour.focus();
            typeDigits(hour, '09');

            expect(document.activeElement).toBe(minute);
        });

        test('should move focus from minute to meridiem once two digits are entered', () => {
            makeContainer('dt-3');
            initialize('dt-3', makeDotNetRef(), '09', '01', '2026', '09', '', '');
            const { minute, meridiem } = segments('dt-3');

            minute.focus();
            typeDigits(minute, '00');

            expect(document.activeElement).toBe(meridiem);
        });
    });

    describe('blur padding — the reported bug', () => {
        // Regression for: a user types "9" into Hour, tabs away, and the segment visually
        // reads "9" — indistinguishable from a filled field. NotifySegmentsChanged requires
        // hour.length === 2, so this silently resolved to a null value with no validation
        // error (the field is optional), even though every segment looked filled in.
        test('should zero-pad a single digit hour on blur', () => {
            makeContainer('dt-4');
            initialize('dt-4', makeDotNetRef(), '', '', '', '', '', '');
            const { hour } = segments('dt-4');

            hour.focus();
            typeDigits(hour, '9');
            hour.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(hour.value).toBe('09');
        });

        test('should zero-pad a single digit minute on blur', () => {
            makeContainer('dt-5');
            initialize('dt-5', makeDotNetRef(), '', '', '', '', '', '');
            const { minute } = segments('dt-5');

            minute.focus();
            typeDigits(minute, '5');
            minute.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(minute.value).toBe('05');
        });

        test('should zero-pad a single digit month on blur', () => {
            makeContainer('dt-6');
            initialize('dt-6', makeDotNetRef(), '', '', '', '', '', '');
            const { month } = segments('dt-6');

            month.focus();
            typeDigits(month, '7');
            month.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(month.value).toBe('07');
        });

        test('should zero-pad a single digit day on blur', () => {
            makeContainer('dt-7');
            initialize('dt-7', makeDotNetRef(), '', '', '', '', '', '');
            const { day } = segments('dt-7');

            day.focus();
            typeDigits(day, '5');
            day.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(day.value).toBe('05');
        });

        test('should not pad the year segment on blur', () => {
            makeContainer('dt-8');
            initialize('dt-8', makeDotNetRef(), '', '', '', '', '', '');
            const { year } = segments('dt-8');

            year.focus();
            typeDigits(year, '6');
            year.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(year.value).toBe('6');
        });

        test('should notify .NET with the padded hour value on blur', () => {
            makeContainer('dt-9');
            const dotNetRef = makeDotNetRef();
            initialize('dt-9', dotNetRef, '09', '01', '2026', '', '00', 'AM');
            const { hour } = segments('dt-9');

            hour.focus();
            typeDigits(hour, '9');
            hour.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith(
                'NotifySegmentsChanged', '09', '01', '2026', '09', '00', 'AM');
        });

        test('should also pad on ":" navigation, not just blur', () => {
            makeContainer('dt-10');
            initialize('dt-10', makeDotNetRef(), '09', '01', '2026', '', '', '');
            const { hour, minute } = segments('dt-10');

            hour.focus();
            typeDigits(hour, '9');
            pressKey(hour, ':');

            expect(hour.value).toBe('09');
            expect(document.activeElement).toBe(minute);
        });
    });

    describe('meridiem interaction', () => {
        test('should toggle AM to PM on ArrowUp', () => {
            makeContainer('dt-11');
            const dotNetRef = makeDotNetRef();
            initialize('dt-11', dotNetRef, '09', '01', '2026', '09', '00', 'AM');
            const { meridiem } = segments('dt-11');

            meridiem.focus();
            pressKey(meridiem, 'ArrowUp');

            expect(meridiem.value).toBe('PM');
        });

        test('should set AM when "a" is pressed', () => {
            makeContainer('dt-12');
            initialize('dt-12', makeDotNetRef(), '09', '01', '2026', '09', '00', '');
            const { meridiem } = segments('dt-12');

            meridiem.focus();
            pressKey(meridiem, 'a');

            expect(meridiem.value).toBe('AM');
        });

        test('should set PM when "p" is pressed', () => {
            makeContainer('dt-13');
            initialize('dt-13', makeDotNetRef(), '09', '01', '2026', '09', '00', '');
            const { meridiem } = segments('dt-13');

            meridiem.focus();
            pressKey(meridiem, 'p');

            expect(meridiem.value).toBe('PM');
        });
    });

    describe('backspace navigation', () => {
        test('should move focus back to minute when backspace is pressed on an empty meridiem segment', () => {
            makeContainer('dt-14');
            initialize('dt-14', makeDotNetRef(), '09', '01', '2026', '09', '00', '');
            const { minute, meridiem } = segments('dt-14');

            meridiem.focus();
            pressKey(meridiem, 'Backspace');

            expect(document.activeElement).toBe(minute);
        });
    });

    describe('setValue / dispose', () => {
        test('setValue should update all six segments for a known container', () => {
            makeContainer('dt-15');
            initialize('dt-15', makeDotNetRef(), '', '', '', '', '', '');

            setValue('dt-15', '12', '25', '2026', '11', '30', 'PM');

            const s = segments('dt-15');
            expect(s.month.value).toBe('12');
            expect(s.day.value).toBe('25');
            expect(s.year.value).toBe('2026');
            expect(s.hour.value).toBe('11');
            expect(s.minute.value).toBe('30');
            expect(s.meridiem.value).toBe('PM');
        });

        test('dispose should remove listeners so further input does not notify .NET', () => {
            makeContainer('dt-16');
            const dotNetRef = makeDotNetRef();
            initialize('dt-16', dotNetRef, '', '', '', '', '', '');
            const { month } = segments('dt-16');

            dispose('dt-16');
            dotNetRef.invokeMethodAsync.mockClear();
            typeDigits(month, '09');

            expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        });
    });
});
