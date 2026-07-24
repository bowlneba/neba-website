const handlersByContainerId = new Map();

export function initialize(containerId, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    const handler = (event) => {
        if (!container.contains(event.target)) {
            dotNetHelper.invokeMethodAsync('NotifyClickedOutside');
        }
    };

    document.addEventListener('mousedown', handler, true);
    handlersByContainerId.set(containerId, handler);
}

export function dispose(containerId) {
    const handler = handlersByContainerId.get(containerId);
    if (!handler) {
        return;
    }

    document.removeEventListener('mousedown', handler, true);
    handlersByContainerId.delete(containerId);
}
