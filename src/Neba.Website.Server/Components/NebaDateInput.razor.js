// Owns all keyboard interaction for NebaDateInput's month/day/year segments, entirely client-side.
// This must not depend on a .NET round-trip to decide focus/navigation — Blazor Server's network
// latency lets fast typing race ahead of a server-driven FocusAsync call, landing digits in the wrong
// segment. JS only reports the composed value back to .NET after the fact (see NotifySegmentsChanged).

const instances = new Map();

const SEGMENT_ORDER = ["month", "day", "year"];
const MAX_LENGTHS = { month: 2, day: 2, year: 4 };

export function initialize(containerId, dotNetHelper, initialMonth, initialDay, initialYear) {
    const container = document.getElementById(containerId);
    const segments = {
        month: container.querySelector('[data-segment="month"]'),
        day: container.querySelector('[data-segment="day"]'),
        year: container.querySelector('[data-segment="year"]')
    };

    segments.month.value = initialMonth;
    segments.day.value = initialDay;
    segments.year.value = initialYear;

    const notify = () => dotNetHelper.invokeMethodAsync(
        "NotifySegmentsChanged", segments.month.value, segments.day.value, segments.year.value);

    const focusSegment = (name) => {
        const input = segments[name];
        input.focus();
        input.select();
    };

    // Month/day accept a single digit while typing (e.g. "7"), but always display
    // zero-padded once the user leaves the segment, matching the mm/dd placeholders.
    const padIfNeeded = (name) => {
        const input = segments[name];
        if ((name === "month" || name === "day") && input.value.length === 1) {
            input.value = "0" + input.value;
        }
    };

    const handleKeyDown = (name, e) => {
        const index = SEGMENT_ORDER.indexOf(name);

        if (e.key === "/" || e.key === "-") {
            e.preventDefault();
            // Only advance if the current segment already has a digit — otherwise a habitual "/"
            // typed right after an auto-advance (e.g. "08/") would skip the next, still-empty segment.
            if (segments[name].value.length > 0 && index < SEGMENT_ORDER.length - 1) {
                padIfNeeded(name);
                focusSegment(SEGMENT_ORDER[index + 1]);
                notify();
            }
            return;
        }

        if (e.key === "Backspace") {
            if (segments[name].value.length === 0 && index > 0) {
                e.preventDefault();
                focusSegment(SEGMENT_ORDER[index - 1]);
            }
            return;
        }

        const isNavigationOrEditingKey = e.key.length > 1 || e.ctrlKey || e.metaKey || e.altKey;
        if (!isNavigationOrEditingKey && !/^[0-9]$/.test(e.key)) {
            e.preventDefault();
        }
    };

    const handleInput = (name) => {
        const input = segments[name];
        const digitsOnly = input.value.replace(/\D/g, "").slice(0, MAX_LENGTHS[name]);
        if (input.value !== digitsOnly) {
            input.value = digitsOnly;
        }

        const index = SEGMENT_ORDER.indexOf(name);
        if (digitsOnly.length === MAX_LENGTHS[name] && index < SEGMENT_ORDER.length - 1) {
            focusSegment(SEGMENT_ORDER[index + 1]);
        }

        notify();
    };

    const listeners = SEGMENT_ORDER.map((name) => {
        const keydown = (e) => handleKeyDown(name, e);
        const input = () => handleInput(name);
        const blur = () => {
            padIfNeeded(name);
            notify();
        };
        segments[name].addEventListener("keydown", keydown);
        segments[name].addEventListener("input", input);
        segments[name].addEventListener("blur", blur);
        return { name, keydown, input, blur };
    });

    instances.set(containerId, { segments, listeners });
}

export function setValue(containerId, month, day, year) {
    const instance = instances.get(containerId);
    if (!instance) {
        return;
    }

    instance.segments.month.value = month;
    instance.segments.day.value = day;
    instance.segments.year.value = year;
}

export function dispose(containerId) {
    const instance = instances.get(containerId);
    if (!instance) {
        return;
    }

    instance.listeners.forEach(({ name, keydown, input, blur }) => {
        instance.segments[name].removeEventListener("keydown", keydown);
        instance.segments[name].removeEventListener("input", input);
        instance.segments[name].removeEventListener("blur", blur);
    });

    instances.delete(containerId);
}
