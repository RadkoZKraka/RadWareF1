import type { ReactNode } from "react";

type InfoCardProps = {
  title: string;
  eyebrow?: string;
  children: ReactNode;
  accent?: "red" | "gold" | "slate";
};

const accentStyles = {
  red: "border-[color:var(--accent)]/20",
  gold: "border-[color:var(--gold)]/35",
  slate: "border-[color:var(--card-border)]",
} as const;

export function InfoCard({
  title,
  eyebrow,
  children,
  accent = "slate",
}: InfoCardProps) {
  return (
    <article
      className={`rounded-[28px] border bg-[var(--card)] p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] backdrop-blur ${accentStyles[accent]}`}
    >
      {eyebrow ? (
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted)]">
          {eyebrow}
        </p>
      ) : null}
      <h3 className="text-xl font-semibold tracking-tight text-[var(--foreground)]">
        {title}
      </h3>
      <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--muted)]">
        {children}
      </div>
    </article>
  );
}
