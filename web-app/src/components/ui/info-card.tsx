import type { ReactNode } from "react";

type InfoCardProps = {
  title: string;
  eyebrow?: string;
  children: ReactNode;
  accent?: "red" | "gold" | "slate";
};

const accentStyles = {
  red: "border-(--card-border) before:bg-(--accent)/18",
  gold: "border-(--card-border) before:bg-(--gold)/18",
  slate: "border-(--card-border) before:bg-(--card-border)",
} as const;

export function InfoCard({
  title,
  eyebrow,
  children,
  accent = "slate",
}: InfoCardProps) {
  return (
    <article
      className={`relative overflow-hidden rounded-[30px] border bg-[linear-gradient(180deg,var(--card-elevated),var(--card))] p-6 shadow-(--shadow-soft) before:absolute before:inset-x-0 before:top-0 before:h-px ${accentStyles[accent]}`}
    >
      {eyebrow ? (
        <p className="mb-3 text-[11px] font-semibold uppercase tracking-[0.3em] text-(--muted)/90">
          {eyebrow}
        </p>
      ) : null}
      <h3 className="text-xl font-semibold tracking-tight text-foreground sm:text-[1.4rem]">
        {title}
      </h3>
      <div className="mt-4 space-y-3 text-sm leading-7 text-(--muted)">
        {children}
      </div>
    </article>
  );
}
