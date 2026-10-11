import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  const html = document.documentElement;

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    html.removeAttribute('data-theme');
  });

  it('should follow the OS and set no data-theme when nothing is stored', () => {
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBeNull();
    expect(html.hasAttribute('data-theme')).toBe(false);
  });

  it('should toggle between light and dark and set data-theme on <html>', () => {
    localStorage.setItem('ttt-theme', 'light');
    const service = TestBed.inject(ThemeService);

    service.toggle();
    expect(service.theme()).toBe('dark');
    expect(html.getAttribute('data-theme')).toBe('dark');

    service.toggle();
    expect(service.theme()).toBe('light');
    expect(html.getAttribute('data-theme')).toBe('light');
  });

  it('should persist the choice in localStorage', () => {
    localStorage.setItem('ttt-theme', 'light');
    const service = TestBed.inject(ThemeService);

    service.toggle();

    expect(localStorage.getItem('ttt-theme')).toBe('dark');
  });

  it('should restore a stored choice on startup', () => {
    localStorage.setItem('ttt-theme', 'dark');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('dark');
    expect(html.getAttribute('data-theme')).toBe('dark');
  });

  it('should ignore an unknown stored value', () => {
    localStorage.setItem('ttt-theme', 'purple');

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBeNull();
  });

  it('should fall back to the OS theme and still toggle when storage throws', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('storage disabled');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('storage disabled');
    });

    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBeNull();

    const before = service.theme();
    expect(() => service.toggle()).not.toThrow();
    expect(service.theme()).not.toBe(before);
    expect(html.getAttribute('data-theme')).toBe(service.theme());
  });
});
