import { describe, test, expect, jest } from '@jest/globals';
import { initialize, dispose } from './NebaAutocomplete.razor.js';

function makeContainer(id) {
    const container = document.createElement('div');
    container.id = id;
    container.innerHTML = '<input />';
    document.body.appendChild(container);
    return container;
}

function makeDotNetRef() {
    return { invokeMethodAsync: jest.fn() };
}

function mousedownOn(target) {
    target.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
}

describe('NebaAutocomplete', () => {
    afterEach(() => {
        document.body.innerHTML = '';
    });

    test('notifies .NET when a mousedown lands outside the container', () => {
        makeContainer('c1');
        const outside = document.createElement('div');
        document.body.appendChild(outside);
        const dotNetRef = makeDotNetRef();

        initialize('c1', dotNetRef);
        mousedownOn(outside);

        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyClickedOutside');

        dispose('c1');
    });

    test('does not notify .NET when a mousedown lands inside the container', () => {
        const container = makeContainer('c2');
        const dotNetRef = makeDotNetRef();

        initialize('c2', dotNetRef);
        mousedownOn(container.querySelector('input'));

        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();

        dispose('c2');
    });

    test('stops listening after dispose', () => {
        const outside = document.createElement('div');
        document.body.appendChild(outside);
        makeContainer('c3');
        const dotNetRef = makeDotNetRef();

        initialize('c3', dotNetRef);
        dispose('c3');
        mousedownOn(outside);

        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('does nothing when initialized with an unknown container id', () => {
        const dotNetRef = makeDotNetRef();

        expect(() => initialize('missing', dotNetRef)).not.toThrow();
    });
});
