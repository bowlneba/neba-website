import { init } from './neba-data-table.js';

jest.mock('./neba-data-table.js', () => ({ init: jest.fn() }));

describe('neba-data-table.start', () => {
    it('starts the responsive table behavior when loaded', async () => {
        await import('./neba-data-table.start.js');

        expect(init).toHaveBeenCalledTimes(1);
    });
});
