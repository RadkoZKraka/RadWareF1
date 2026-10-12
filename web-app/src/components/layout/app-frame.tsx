import type { ReactNode } from "react";
import { ThemeToggle } from "@/components/theme/theme-toggle";

type AppFrameProps = {
  children: ReactNode;
};

export function AppFrame({ children }: AppFrameProps) {
  return (
    <div className="app-grid relative min-h-screen overflow-hidden">
      <div className="pointer-events-none absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(255,77,79,0.22),transparent_58%)]" />
      <div className="pointer-events-none absolute -right-48 top-24 h-80 w-80 rounded-full bg-(--gold)/10 blur-3xl" />
      <div className="pointer-events-none absolute -left-40 top-96 h-72 w-72 rounded-full bg-(--accent)/10 blur-3xl" />
      <div className="relative mx-auto flex min-h-screen w-full max-w-7xl flex-col px-6 py-6 sm:px-8 lg:px-10">
        <header className="sticky top-6 z-20 mb-8 flex justify-center">
          <div className="flex w-full max-w-5xl items-center justify-between rounded-full border border-white/12 bg-white/10 px-4 py-3 shadow-[0_20px_60px_rgba(15,23,42,0.16)] backdrop-blur-2xl sm:px-6">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[linear-gradient(135deg,var(--accent),var(--accent-strong))] text-sm font-semibold text-white shadow-[0_12px_30px_rgba(255,77,79,0.3)]">
                F1
              </div>
              <div>
                <p className="text-sm font-semibold tracking-[-0.02em] text-foreground">
                  F1Shots Race Club
                </p>
                <p className="text-xs text-(--muted)">
                  Predictions, private leagues, race-day rivalry.
                </p>
              </div>
            </div>
            <ThemeToggle />
          </div>
        </header>
        {children}
      </div>
    </div>
  );
}
