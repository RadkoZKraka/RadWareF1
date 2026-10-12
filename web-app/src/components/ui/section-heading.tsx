type SectionHeadingProps = {
  eyebrow: string;
  title: string;
  description: string;
};

export function SectionHeading({
  eyebrow,
  title,
  description,
}: SectionHeadingProps) {
  return (
    <div className="max-w-2xl space-y-5">
      <p className="text-[11px] font-semibold uppercase tracking-[0.34em] text-(--gold)">
        {eyebrow}
      </p>
      <h2 className="text-3xl font-semibold tracking-[-0.04em] text-foreground sm:text-4xl lg:text-[2.8rem]">
        {title}
      </h2>
      <p className="text-base leading-8 text-(--muted) sm:text-lg">
        {description}
      </p>
    </div>
  );
}
