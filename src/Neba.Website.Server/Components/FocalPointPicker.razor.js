const instances = new Map();

export const clamp = (value) => Math.max(0, Math.min(100, value));

/** Size of the visible window as a fraction of the photo's width and height. Zooming in shrinks it. */
export function cropSize(photoAspect, shapeAspect, zoom) {
    const zoomFactor = zoom / 100;
    return {
        cw: Math.min(1, shapeAspect / photoAspect) / zoomFactor,
        ch: Math.min(1, photoAspect / shapeAspect) / zoomFactor,
    };
}

/** Center of the visible window, as a fraction of the photo, for object-position percents x and y. */
export function centerOf(x, y, size) {
    return {
        cx: size.cw / 2 + (x / 100) * (1 - size.cw),
        cy: size.ch / 2 + (y / 100) * (1 - size.ch),
    };
}

/**
 * The object-position percents that put the window's center at (cx, cy), as near as the photo allows.
 * An axis with no overflow cannot move and stays at 50.
 */
export function positionFor(cx, cy, size) {
    return {
        x: size.cw < 1 ? clamp(Math.round(((cx - size.cw / 2) / (1 - size.cw)) * 100)) : 50,
        y: size.ch < 1 ? clamp(Math.round(((cy - size.ch / 2) / (1 - size.ch)) * 100)) : 50,
    };
}

/** The same inline style FocalPoint.ToImageStyle renders, so the preview can follow a drag without a round trip. */
export function imageStyle(x, y, zoom) {
    return `object-position:${x}% ${y}%;transform-origin:${x}% ${y}%;transform:scale(${zoom / 100})`;
}

export function initialize(rootId, dotNetHelper, x, y, zoom) {
    const root = document.getElementById(rootId);
    if (!root) {
        return;
    }

    const surface = root.querySelector('[data-fp-surface]');
    const image = surface.querySelector('img');
    const instance = {
        root,
        surface,
        image,
        crop: surface.querySelector('[data-fp-crop]'),
        marker: surface.querySelector('[data-fp-marker]'),
        slider: root.querySelector('[data-fp-zoom]'),
        preview: root.querySelector('[data-fp-preview]'),
        zoomText: root.querySelector('[data-fp-zoom-text]'),
        dotNetHelper,
        x,
        y,
        zoom,
        photoAspect: image.naturalWidth > 0 ? image.naturalWidth / image.naturalHeight : 4 / 3,
        shapeAspect: Number(root.dataset.aspect) || 1,
        isCircle: root.dataset.shape === 'circle',
        drag: null,
        movedDuringDrag: false,
        notifyQueued: false,
        cleanups: [],
    };

    const on = (target, type, handler) => {
        target.addEventListener(type, handler);
        instance.cleanups.push(() => target.removeEventListener(type, handler));
    };

    on(image, 'load', () => {
        instance.photoAspect = image.naturalWidth / image.naturalHeight;
        render(instance);
    });
    on(surface, 'pointerdown', (event) => pointerDown(instance, event));
    on(surface, 'pointermove', (event) => pointerMove(instance, event));
    on(surface, 'pointerup', (event) => endDrag(instance, event));
    on(surface, 'pointercancel', (event) => endDrag(instance, event));
    on(surface, 'pointerleave', () => {
        if (!instance.drag) {
            surface.classList.remove('focal-picker__surface--over-crop');
        }
    });
    on(surface, 'keydown', (event) => keyDown(instance, event));
    on(instance.slider, 'input', (event) => zoomChanged(instance, Number(event.target.value)));
    on(instance.slider, 'change', () => notify(instance));

    instances.set(rootId, instance);
    render(instance);
}

export function update(rootId, x, y, zoom) {
    const instance = instances.get(rootId);
    if (!instance) {
        return;
    }

    instance.x = x;
    instance.y = y;
    instance.zoom = zoom;
    render(instance);
}

export function dispose(rootId) {
    const instance = instances.get(rootId);
    if (!instance) {
        return;
    }

    instance.cleanups.forEach((cleanup) => cleanup());
    instances.delete(rootId);
}

function size(instance) {
    return cropSize(instance.photoAspect, instance.shapeAspect, instance.zoom);
}

function render(instance) {
    const crop = size(instance);
    const { cx, cy } = centerOf(instance.x, instance.y, crop);

    instance.surface.style.setProperty('--fp-aspect', String(instance.photoAspect));
    instance.crop.style.width = `${crop.cw * 100}%`;
    instance.crop.style.height = `${crop.ch * 100}%`;
    instance.crop.style.left = `${cx * 100}%`;
    instance.crop.style.top = `${cy * 100}%`;
    instance.crop.style.borderRadius = instance.isCircle ? '50%' : '4px';
    instance.marker.style.left = `${cx * 100}%`;
    instance.marker.style.top = `${cy * 100}%`;
    instance.slider.value = String(instance.zoom);

    // The preview and zoom label live in .NET too, but a drag only tells .NET when it ends; these keep them live.
    if (instance.preview) {
        instance.preview.setAttribute('style', imageStyle(instance.x, instance.y, instance.zoom));
    }

    if (instance.zoomText) {
        instance.zoomText.textContent = `${(instance.zoom / 100).toFixed(1)}×`;
    }
}

/** Sends the latest value to .NET at most once per animation frame, so a fast drag does not flood SignalR. */
function notify(instance) {
    if (instance.notifyQueued) {
        return;
    }

    instance.notifyQueued = true;
    requestAnimationFrame(() => {
        instance.notifyQueued = false;
        instance.dotNetHelper.invokeMethodAsync('NotifyChanged', instance.x, instance.y, instance.zoom);
    });
}

function photoPoint(instance, event) {
    const rect = instance.surface.getBoundingClientRect();
    return { fx: (event.clientX - rect.left) / rect.width, fy: (event.clientY - rect.top) / rect.height };
}

function insideCrop(instance, fx, fy) {
    const crop = size(instance);
    const { cx, cy } = centerOf(instance.x, instance.y, crop);
    const nx = (fx - cx) / (crop.cw / 2);
    const ny = (fy - cy) / (crop.ch / 2);
    return instance.isCircle ? nx * nx + ny * ny <= 1 : Math.abs(nx) <= 1 && Math.abs(ny) <= 1;
}

function moveCenterTo(instance, cx, cy) {
    const next = positionFor(cx, cy, size(instance));
    if (next.x === instance.x && next.y === instance.y) {
        return;
    }

    instance.x = next.x;
    instance.y = next.y;
    render(instance);

    // During a drag only the final position goes to .NET (see endDrag), not every frame of it.
    if (instance.drag) {
        instance.movedDuringDrag = true;
    } else {
        notify(instance);
    }
}

// A click jumps the window to that point; pressing on the window and dragging moves it from where it was
// grabbed, so it does not jump under the pointer.
function pointerDown(instance, event) {
    if (event.button !== 0) {
        return;
    }

    const { fx, fy } = photoPoint(instance, event);
    if (insideCrop(instance, fx, fy)) {
        const { cx, cy } = centerOf(instance.x, instance.y, size(instance));
        instance.drag = { dx: cx - fx, dy: cy - fy };
    } else {
        instance.drag = { dx: 0, dy: 0 };
        moveCenterTo(instance, fx, fy);
    }

    instance.surface.setPointerCapture?.(event.pointerId);
    instance.surface.classList.add('focal-picker__surface--dragging');
    instance.surface.focus({ preventScroll: true });
}

function pointerMove(instance, event) {
    const { fx, fy } = photoPoint(instance, event);
    if (!instance.drag) {
        instance.surface.classList.toggle('focal-picker__surface--over-crop', insideCrop(instance, fx, fy));
        return;
    }

    moveCenterTo(instance, fx + instance.drag.dx, fy + instance.drag.dy);
}

function endDrag(instance, event) {
    if (!instance.drag) {
        return;
    }

    instance.drag = null;
    instance.surface.classList.remove('focal-picker__surface--dragging');
    if (instance.movedDuringDrag) {
        instance.movedDuringDrag = false;
        notify(instance);
    }

    if (instance.surface.hasPointerCapture?.(event.pointerId)) {
        instance.surface.releasePointerCapture(event.pointerId);
    }
}

const KEY_DIRECTIONS = {
    ArrowLeft: [-1, 0],
    ArrowRight: [1, 0],
    ArrowUp: [0, -1],
    ArrowDown: [0, 1],
};

function keyDown(instance, event) {
    const direction = KEY_DIRECTIONS[event.key];
    if (!direction) {
        return;
    }

    event.preventDefault();
    const step = event.shiftKey ? 5 : 1;
    const crop = size(instance);
    if (crop.cw < 1) {
        instance.x = clamp(instance.x + direction[0] * step);
    }

    if (crop.ch < 1) {
        instance.y = clamp(instance.y + direction[1] * step);
    }

    render(instance);
    notify(instance);
}

// Zooming keeps the center of the window where it is, then re-derives the position percents. The slider's
// "change" event (release) tells .NET; "input" (every step of a drag) only redraws.
function zoomChanged(instance, zoom) {
    const before = centerOf(instance.x, instance.y, size(instance));
    instance.zoom = zoom;
    const next = positionFor(before.cx, before.cy, size(instance));
    instance.x = next.x;
    instance.y = next.y;
    render(instance);
}
