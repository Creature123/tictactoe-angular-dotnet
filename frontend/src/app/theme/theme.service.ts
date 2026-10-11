import { computed, DOCUMENT, inject, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'ttt-theme';

/**
 * Light/dark theme. With no explicit choice the CSS follows the OS (prefers-color-scheme);
 * once the user toggles, data-theme on <html> overrides it and the choice is saved.
 * Storage is best-effort: if it is unavailable the choice lasts for the session only.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  /** The user's explicit choice, or null to follow the OS. */
  readonly preference = signal<Theme | null>(this.readStored());

  /** The theme currently shown. */
  readonly theme = computed<Theme>(() => this.preference() ?? this.osTheme());

  constructor() {
    const stored = this.preference();
    if (stored) {
      this.apply(stored);
    }
  }

  toggle(): void {
    const next: Theme = this.theme() === 'dark' ? 'light' : 'dark';
    this.preference.set(next);
    this.apply(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Storage unavailable (private mode, blocked): keep the in-memory choice.
    }
  }

  private apply(theme: Theme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
  }

  private osTheme(): Theme {
    return this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
  }

  private readStored(): Theme | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return value === 'light' || value === 'dark' ? value : null;
    } catch {
      return null;
    }
  }
}
