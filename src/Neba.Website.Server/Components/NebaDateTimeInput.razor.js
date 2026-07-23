// Owns all keyboard interaction for NebaDateTimeInput's month/day/year/hour/minute/meridiem
// segments, entirely client-side — same rationale as NebaDateInput.razor.js: Blazor Server's
// network latency lets fast typing race ahead of a server-driven FocusAsync call, landing digits
// in the wrong segment. JS only reports the composed value back to .NET after the fact.

const instances = new Map();

const SEGMENT_ORDER = ["month", "day", "year", "hour", "minute", "meridiem"];
const MAX_LENGTHS = { month: 2, day: 2, year: 4, hour: 2, minute: 2, meridiem: 2 };
const NUMERIC_SEGMENTS = new Set(["month", "day", "year", "hour", "minute"]);
// year is excluded — a partial year isn't safe to zero-pad into a specific year.
const PAD_SEGMENTS = new Set(["month", "day", "hour", "minute"]);

export function initialize(containerId, dotNetHelper, initialMonth, initialDay, initialYear, initialHour, initialMinute, initialMeridiem) {
    const container = document.getElementById(containerId);
    const segments = {
        month: container.querySelector('[data-segment="month"]'),
        day: container.querySelector('[data-segment="day"]'),
        year: container.querySelector('[data-segment="year"]'),
        hour: container.querySelector('[data-segment="hour"]'),
        minute: container.querySelector('[data-segment="minute"]'),
        meridiem: container.querySelector('[data-segment="meridiem"]')
    };

    segments.month.value = initialMonth;
    segments.day.value = initialDay;
    segments.year.value = initialYear;
    segments.hour.value = initialHour;
    segments.minute.value = initialMinute;
    segments.meridiem.value = initialMeridiem;

    const notify = () => dotNetHelper.invokeMethodAsync(
        "NotifySegmentsChanged",
        segments.month.value, segments.day.value, segments.year.value,
        segments.hour.value, segments.minute.value, segments.meridiem.value);

    const focusSegment = (name) => {
        const input = segments[name];
        input.focus();
        input.select();
    };

    const setMeridiem = (value) => {
        segments.meridiem.value = value;
        notify();
    };

    // Without this, a single typed digit (e.g. "9" left un-padded) fails
    // NotifySegmentsChanged's all-segments-length check and silently resolves to a null value
    // with no visible error, since the field is optional — see NebaDateInput's matching padIfNeeded.
    const padIfNeeded = (name) => {
        const input = segments[name];
        if (PAD_SEGMENTS.has(name) && input.value.length === 1) {
            input.value = "0" + input.value;
        }
    };

    const handleKeyDown = (name, e) => {
        const index = SEGMENT_ORDER.indexOf(name);

        if (name === "meridiem") {
            if (e.key === "ArrowUp" || e.key === "ArrowDown") {
                e.preventDefault();
                setMeridiem(segments.meridiem.value === "AM" ? "PM" : "AM");
                return;
            }

            if (/^[aA]$/.test(e.key)) {
                e.preventDefault();
                setMeridiem("AM");
                return;
            }

            if (/^[pP]$/.test(e.key)) {
                e.preventDefault();
                setMeridiem("PM");
                return;
            }

            if (e.key === "Backspace" && segments.meridiem.value.length === 0 && index > 0) {
                e.preventDefault();
                focusSegment(SEGMENT_ORDER[index - 1]);
                return;
            }

            const isNavigationKey = e.key.length > 1 || e.ctrlKey || e.metaKey || e.altKey;
            if (!isNavigationKey) {
                e.preventDefault();
            }
            return;
        }

        if (e.key === "/" || e.key === "-" || e.key === ":") {
            e.preventDefault();
            // Only advance if the current segment already has a digit — otherwise a habitual
            // separator typed right after an auto-advance would skip the next, still-empty segment.
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
        if (!NUMERIC_SEGMENTS.has(name)) {
            return;
        }

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

export function setValue(containerId, month, day, year, hour, minute, meridiem) {
    const instance = instances.get(containerId);
    if (!instance) {
        return;
    }

    instance.segments.month.value = month;
    instance.segments.day.value = day;
    instance.segments.year.value = year;
    instance.segments.hour.value = hour;
    instance.segments.minute.value = minute;
    instance.segments.meridiem.value = meridiem;
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
