"use client";

import { useEffect, useSyncExternalStore } from "react";

type ThemeMode = "light" | "dark" | "system";

const storageKey = "f1shots-theme";
const themeChangeEvent = "f1shots-theme-change";
const modes: ThemeMode[] = ["light", "dark", "system"];

function getStoredThemeMode(): ThemeMode {
  if (typeof window === "undefined") {
    return "system";
  }

  const value = localStorage.getItem(storageKey);
  return value === "light" || value === "dark" || value === "system"
    ? value
    : "system";
}

function resolveTheme(mode: ThemeMode) {
  if (mode === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }

  return mode;
}

function applyTheme(mode: ThemeMode) {
  const root = document.documentElement;
  const resolvedTheme = resolveTheme(mode);

  root.dataset.theme = mode;
  root.dataset.resolvedTheme = resolvedTheme;
  localStorage.setItem(storageKey, mode);
}

function transitionTheme(update: () => void) {
  const doc = document as Document & {
    startViewTransition?: (callback: () => void) => {
      finished: Promise<void>;
    };
  };

  if (doc.startViewTransition) {
    doc.startViewTransition(update);
    return;
  }

  update();
}

function subscribe(onStoreChange: () => void) {
  if (typeof window === "undefined") {
    return () => undefined;
  }

  const handleStorage = (event: StorageEvent) => {
    if (event.key === storageKey) {
      onStoreChange();
    }
  };

  const handleThemeChange = () => onStoreChange();

  window.addEventListener("storage", handleStorage);
  window.addEventListener(themeChangeEvent, handleThemeChange);

  return () => {
    window.removeEventListener("storage", handleStorage);
    window.removeEventListener(themeChangeEvent, handleThemeChange);
  };
}

function getServerSnapshot(): ThemeMode {
  return "system";
}

function persistThemeMode(mode: ThemeMode) {
  transitionTheme(() => {
    applyTheme(mode);
    window.dispatchEvent(new Event(themeChangeEvent));
  });
}

export function ThemeToggle() {
  const mode = useSyncExternalStore(subscribe, getStoredThemeMode, getServerSnapshot);

  useEffect(() => {
    applyTheme(mode);

    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    const handleChange = () => {
      if (getStoredThemeMode() === "system") {
        transitionTheme(() => {
          applyTheme("system");
          window.dispatchEvent(new Event(themeChangeEvent));
        });
      }
    };

    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, [mode]);

  return (
    <div className="flex items-center gap-1 rounded-full border border-white/12 bg-white/10 p-1 shadow-[0_10px_30px_rgba(0,0,0,0.18)] backdrop-blur-xl">
      {modes.map((option) => {
        const active = mode === option;

        return (
          <button
            key={option}
            type="button"
            onClick={() => {
              persistThemeMode(option);
            }}
            className={`rounded-full px-3 py-2 text-xs font-medium capitalize transition sm:px-4 ${
              active
                ? "bg-white text-[#0b1118]"
                : "text-(--muted) hover:text-foreground"
            }`}
          >
            {option}
          </button>
        );
      })}
    </div>
  );
}



