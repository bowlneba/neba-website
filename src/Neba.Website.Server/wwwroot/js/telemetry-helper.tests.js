// Tests for telemetry-helper.js
// Covers: initializeTelemetry, trackEvent, trackError, createTimer,
//         withTelemetry, trackResourcePerformance, trackNavigationPerformance,
//         trackWebVitals, initializePerformanceTracking, globalThis.telemetry

import {
  initializeTelemetry,
  trackEvent,
  trackError,
  createTimer,
  withTelemetry,
  trackResourcePerformance,
  trackNavigationPerformance,
  trackWebVitals,
  initializePerformanceTracking,
} from './telemetry-helper.js';

describe('telemetry-helper', () => {
  let mockDotNet;

  beforeEach(() => {
    mockDotNet = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
    initializeTelemetry(null); // reset module-level bridge between tests
  });

  afterEach(() => {
    jest.restoreAllMocks();
    jest.clearAllTimers();
  });

  // ---------------------------------------------------------------------------
  describe('initializeTelemetry', () => {
    test('enables the bridge so trackEvent reaches .NET', () => {
      initializeTelemetry(mockDotNet);
      trackEvent('test.event');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'test.event', {});
    });

    test('passing null disables the bridge', () => {
      initializeTelemetry(mockDotNet);
      initializeTelemetry(null);
      trackEvent('test.event');
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------
  describe('trackEvent', () => {
    test('invokes TrackEvent with event name and properties', () => {
      initializeTelemetry(mockDotNet);
      trackEvent('map.route_calculated', { duration_ms: 150, success: true });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'map.route_calculated',
        { duration_ms: 150, success: true },
      );
    });

    test('defaults properties to empty object', () => {
      initializeTelemetry(mockDotNet);
      trackEvent('page.viewed');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'page.viewed', {});
    });

    test('does nothing when bridge is not initialised', () => {
      expect(() => trackEvent('test.event')).not.toThrow();
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('suppresses errors thrown by the bridge', () => {
      jest.spyOn(console, 'warn').mockImplementation(() => {});
      mockDotNet.invokeMethodAsync.mockImplementation(() => {
        throw new Error('interop failure');
      });
      initializeTelemetry(mockDotNet);
      expect(() => trackEvent('test.event')).not.toThrow();
    });
  });

  // ---------------------------------------------------------------------------
  describe('trackError', () => {
    test('invokes TrackError with message, source, and stack trace', () => {
      initializeTelemetry(mockDotNet);
      trackError('Something failed', 'map.route', 'Error: Something failed\n  at test');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackError',
        'Something failed',
        'map.route',
        'Error: Something failed\n  at test',
      );
    });

    test('defaults stack trace to null', () => {
      initializeTelemetry(mockDotNet);
      trackError('Error occurred', 'map.route');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackError',
        'Error occurred',
        'map.route',
        null,
      );
    });

    test('does nothing when bridge is not initialised', () => {
      expect(() => trackError('Error', 'source')).not.toThrow();
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('suppresses errors thrown by the bridge', () => {
      jest.spyOn(console, 'warn').mockImplementation(() => {});
      mockDotNet.invokeMethodAsync.mockImplementation(() => {
        throw new Error('interop failure');
      });
      initializeTelemetry(mockDotNet);
      expect(() => trackError('err', 'src')).not.toThrow();
    });
  });

  // ---------------------------------------------------------------------------
  describe('createTimer', () => {
    beforeEach(() => {
      initializeTelemetry(mockDotNet);
    });

    test('returns an object with a stop() method', () => {
      const timer = createTimer('test.event');
      expect(typeof timer.stop).toBe('function');
    });

    test('stop() tracks event with elapsed duration_ms', () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(1000).mockReturnValueOnce(1150);
      const timer = createTimer('map.load');
      timer.stop();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'map.load',
        expect.objectContaining({ duration_ms: 150 }),
      );
    });

    test('stop() defaults success to true', () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(0).mockReturnValueOnce(100);
      createTimer('map.load').stop();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'map.load',
        expect.objectContaining({ success: true }),
      );
    });

    test('stop(false) tracks success: false', () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(0).mockReturnValueOnce(100);
      createTimer('map.load').stop(false);
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'map.load',
        expect.objectContaining({ success: false }),
      );
    });

    test('stop() merges additional properties into the event', () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(0).mockReturnValueOnce(100);
      createTimer('map.load').stop(true, { route_id: 'abc', tiles: 12 });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'map.load',
        expect.objectContaining({ route_id: 'abc', tiles: 12 }),
      );
    });

    test('stop() returns the elapsed duration', () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(1000).mockReturnValueOnce(1250);
      expect(createTimer('map.load').stop()).toBe(250);
    });
  });

  // ---------------------------------------------------------------------------
  describe('withTelemetry', () => {
    beforeEach(() => {
      initializeTelemetry(mockDotNet);
    });

    test('returns a function', () => {
      expect(typeof withTelemetry('test.event', async () => {})).toBe('function');
    });

    test('returns the result of the wrapped function', async () => {
      const wrapped = withTelemetry('test.event', async () => 'expected');
      await expect(wrapped()).resolves.toBe('expected');
    });

    test('forwards arguments to the wrapped function', async () => {
      const fn = jest.fn().mockResolvedValue('ok');
      await withTelemetry('test.event', fn)('arg1', 42);
      expect(fn).toHaveBeenCalledWith('arg1', 42);
    });

    test('tracks event with success: true and duration on success', async () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(0).mockReturnValueOnce(300);
      await withTelemetry('op.done', async () => {})();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'op.done',
        expect.objectContaining({ success: true, duration_ms: 300 }),
      );
    });

    test('tracks event with success: false and error message on failure', async () => {
      jest.spyOn(performance, 'now').mockReturnValueOnce(0).mockReturnValueOnce(100);
      await expect(
        withTelemetry('op.fail', async () => {
          throw new Error('oops');
        })(),
      ).rejects.toThrow('oops');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'op.fail',
        expect.objectContaining({ success: false, error: 'oops' }),
      );
    });

    test('calls trackError with message, event name, and stack on failure', async () => {
      const err = new Error('boom');
      err.stack = 'Error: boom\n  at test:1:1';
      await expect(
        withTelemetry('op.fail', async () => {
          throw err;
        })(),
      ).rejects.toThrow('boom');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackError',
        'boom',
        'op.fail',
        'Error: boom\n  at test:1:1',
      );
    });

    test('re-throws the original error instance', async () => {
      const original = new Error('original');
      await expect(
        withTelemetry('test.event', async () => {
          throw original;
        })(),
      ).rejects.toBe(original);
    });
  });

  // ---------------------------------------------------------------------------
  describe('trackResourcePerformance', () => {
    const makeEntry = ({ url = 'https://example.com/app.js', type = 'script', duration = 80 } = {}) => ({
      name: url,
      initiatorType: type,
      duration,
      transferSize: 5000,
      domainLookupStart: 0,
      domainLookupEnd: 5,
      connectStart: 5,
      connectEnd: 20,
      requestStart: 20,
      responseStart: 60,
      responseEnd: 80,
    });

    beforeEach(() => {
      initializeTelemetry(mockDotNet);
      // jsdom does not implement Resource Timing API — add stubs so spying works
      performance.getEntriesByType = jest.fn().mockReturnValue([]);
      performance.clearResourceTimings = jest.fn();
    });

    test('tracks a resource.loaded event for each resource entry', () => {
      performance.getEntriesByType.mockReturnValue([
        makeEntry({ url: 'https://example.com/app.js', type: 'script' }),
        makeEntry({ url: 'https://example.com/style.css', type: 'link' }),
      ]);
      trackResourcePerformance();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    test('extracts resource_name from the URL pathname', () => {
      performance.getEntriesByType.mockReturnValue([
        makeEntry({ url: 'https://cdn.example.com/assets/app.min.js' }),
      ]);
      trackResourcePerformance();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'resource.loaded',
        expect.objectContaining({ resource_name: 'app.min.js' }),
      );
    });

    test('calculates timing metrics correctly', () => {
      performance.getEntriesByType.mockReturnValue([makeEntry()]);
      trackResourcePerformance();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'resource.loaded',
        expect.objectContaining({
          duration_ms: 80,
          transfer_size: 5000,
          dns_time: 5,
          tcp_time: 15,
          ttfb: 40,
          download_time: 20,
        }),
      );
    });

    test('filters by initiatorType when resourceType argument is provided', () => {
      performance.getEntriesByType.mockReturnValue([
        makeEntry({ type: 'script' }),
        makeEntry({ url: 'https://example.com/style.css', type: 'link' }),
      ]);
      trackResourcePerformance('script');
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledTimes(1);
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'resource.loaded',
        expect.objectContaining({ resource_type: 'script' }),
      );
    });

    test('clears the resource timing buffer after tracking', () => {
      trackResourcePerformance();
      expect(performance.clearResourceTimings).toHaveBeenCalled();
    });

    test('warns and returns early when Performance API is unavailable', () => {
      const consoleSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
      performance.getEntriesByType = undefined;
      trackResourcePerformance();
      expect(consoleSpy).toHaveBeenCalledWith('[Telemetry] Performance API not available');
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------
  describe('trackNavigationPerformance', () => {
    const makeNavEntry = (overrides = {}) => ({
      type: 'navigate',
      redirectCount: 0,
      domainLookupStart: 0,
      domainLookupEnd: 5,
      connectStart: 5,
      connectEnd: 20,
      requestStart: 20,
      responseStart: 60,
      responseEnd: 80,
      domLoading: 80,
      domInteractive: 200,
      domComplete: 500,
      domContentLoadedEventStart: 200,
      domContentLoadedEventEnd: 210,
      loadEventStart: 500,
      loadEventEnd: 510,
      navigationStart: 0,
      ...overrides,
    });

    beforeEach(() => {
      initializeTelemetry(mockDotNet);
      // jsdom does not implement Navigation Timing API — add stub so tests can control the return value
      performance.getEntriesByType = jest.fn().mockReturnValue([]);
    });

    test('tracks page.performance event', () => {
      performance.getEntriesByType.mockReturnValue([makeNavEntry()]);
      trackNavigationPerformance();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'page.performance',
        expect.any(Object),
      );
    });

    test('includes all calculated navigation metrics', () => {
      performance.getEntriesByType.mockReturnValue([makeNavEntry()]);
      trackNavigationPerformance();
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith(
        'TrackEvent',
        'page.performance',
        expect.objectContaining({
          navigation_type: 'navigate',
          redirect_count: 0,
          dns_time: 5,
          tcp_time: 15,
          request_time: 40,
          response_time: 20,
          dom_processing_time: 420,
          dom_interactive_time: 120,
          dom_content_loaded_time: 10,
          load_event_time: 10,
          total_load_time: 510,
        }),
      );
    });

    test('defers via load listener when document is not complete', () => {
      // jsdom exposes readyState as an accessor on Document.prototype
      jest.spyOn(Document.prototype, 'readyState', 'get').mockReturnValue('loading');
      const addEventListenerSpy = jest.spyOn(window, 'addEventListener');
      trackNavigationPerformance();
      expect(addEventListenerSpy).toHaveBeenCalledWith('load', expect.any(Function));
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('warns when navigation timing data is absent', () => {
      const consoleSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
      trackNavigationPerformance();
      expect(consoleSpy).toHaveBeenCalledWith('[Telemetry] Navigation timing data not available');
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('warns and returns early when Navigation Timing API is unavailable', () => {
      const consoleSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
      performance.getEntriesByType = undefined;
      trackNavigationPerformance();
      expect(consoleSpy).toHaveBeenCalledWith('[Telemetry] Navigation Timing API not available');
    });
  });

  // ---------------------------------------------------------------------------
  describe('trackWebVitals', () => {
    let observers;
    let OriginalPerformanceObserver;

    beforeEach(() => {
      initializeTelemetry(mockDotNet);
      OriginalPerformanceObserver = globalThis.PerformanceObserver;
      observers = [];
      globalThis.PerformanceObserver = jest.fn().mockImplementation((callback) => {
        const observer = { observe: jest.fn(), callback };
        observers.push(observer);
        return observer;
      });
    });

    afterEach(() => {
      globalThis.PerformanceObserver = OriginalPerformanceObserver;
    });

    test('observes LCP, FID, and CLS entry types', () => {
      trackWebVitals();
      expect(observers[0].observe).toHaveBeenCalledWith({ entryTypes: ['largest-contentful-paint'] });
      expect(observers[1].observe).toHaveBeenCalledWith({ entryTypes: ['first-input'] });
      expect(observers[2].observe).toHaveBeenCalledWith({ entryTypes: ['layout-shift'] });
    });

    test('tracks web_vitals.lcp using renderTime when available', () => {
      trackWebVitals();
      observers[0].callback({
        getEntries: () => [
          { renderTime: 800, loadTime: 700, element: { tagName: 'H1' } },
          { renderTime: 1200, loadTime: 1100, element: { tagName: 'IMG' } },
        ],
      });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'web_vitals.lcp', {
        value: 1200,
        element: 'IMG',
      });
    });

    test('tracks web_vitals.lcp falling back to loadTime when renderTime is 0', () => {
      trackWebVitals();
      observers[0].callback({
        getEntries: () => [{ renderTime: 0, loadTime: 1100, element: null }],
      });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'web_vitals.lcp', {
        value: 1100,
        element: 'unknown',
      });
    });

    test('tracks web_vitals.fid for each first-input entry', () => {
      trackWebVitals();
      observers[1].callback({
        getEntries: () => [
          { processingStart: 100, startTime: 90, name: 'click' },
          { processingStart: 200, startTime: 180, name: 'keydown' },
        ],
      });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'web_vitals.fid', {
        value: 10,
        event_type: 'click',
      });
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'web_vitals.fid', {
        value: 20,
        event_type: 'keydown',
      });
    });

    test('accumulates CLS shifts that do not have recent input', () => {
      trackWebVitals();
      observers[2].callback({
        getEntries: () => [
          { value: 0.1, hadRecentInput: false },
          { value: 0.05, hadRecentInput: true }, // ignored
          { value: 0.15, hadRecentInput: false },
        ],
      });
      Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true });
      globalThis.dispatchEvent(new Event('visibilitychange'));
      expect(mockDotNet.invokeMethodAsync).toHaveBeenCalledWith('TrackEvent', 'web_vitals.cls', {
        value: expect.closeTo(0.25),
      });
    });

    test('does not track CLS when page is still visible', () => {
      trackWebVitals();
      Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true });
      globalThis.dispatchEvent(new Event('visibilitychange'));
      expect(mockDotNet.invokeMethodAsync).not.toHaveBeenCalledWith(
        'TrackEvent',
        'web_vitals.cls',
        expect.anything(),
      );
    });

    test('does nothing when PerformanceObserver is not available', () => {
      delete globalThis.PerformanceObserver;
      expect(() => trackWebVitals()).not.toThrow();
    });

    test('warns when PerformanceObserver throws during setup', () => {
      globalThis.PerformanceObserver = jest.fn().mockImplementation(() => {
        throw new Error('Observer unavailable');
      });
      const consoleSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
      trackWebVitals();
      expect(consoleSpy).toHaveBeenCalledWith(
        '[Telemetry] Failed to track Web Vitals:',
        expect.any(Error),
      );
    });
  });

  // ---------------------------------------------------------------------------
  describe('initializePerformanceTracking', () => {
    beforeEach(() => {
      jest.useFakeTimers();
      initializeTelemetry(mockDotNet);
      performance.getEntriesByType = jest.fn().mockReturnValue([]);
      performance.clearResourceTimings = jest.fn();
      jest.spyOn(console, 'warn').mockImplementation(() => {});
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    test('calls trackNavigationPerformance immediately', () => {
      initializePerformanceTracking();
      // absence of nav entry triggers the warning, proving it ran synchronously
      expect(console.warn).toHaveBeenCalledWith('[Telemetry] Navigation timing data not available');
    });

    test('schedules trackResourcePerformance after 1000 ms', () => {
      jest.spyOn(performance, 'clearResourceTimings').mockImplementation(() => {});
      initializePerformanceTracking();
      expect(performance.clearResourceTimings).not.toHaveBeenCalled();
      jest.advanceTimersByTime(1000);
      expect(performance.clearResourceTimings).toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------
  describe('globalThis.telemetry', () => {
    test('exposes all exported functions', () => {
      expect(globalThis.telemetry).toBeDefined();
      expect(globalThis.telemetry.initializeTelemetry).toBe(initializeTelemetry);
      expect(globalThis.telemetry.trackEvent).toBe(trackEvent);
      expect(globalThis.telemetry.trackError).toBe(trackError);
      expect(globalThis.telemetry.createTimer).toBe(createTimer);
      expect(globalThis.telemetry.withTelemetry).toBe(withTelemetry);
      expect(globalThis.telemetry.trackResourcePerformance).toBe(trackResourcePerformance);
      expect(globalThis.telemetry.trackNavigationPerformance).toBe(trackNavigationPerformance);
      expect(globalThis.telemetry.trackWebVitals).toBe(trackWebVitals);
      expect(globalThis.telemetry.initializePerformanceTracking).toBe(initializePerformanceTracking);
    });
  });
});
