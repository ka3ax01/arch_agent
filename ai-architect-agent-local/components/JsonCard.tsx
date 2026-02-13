import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

type JsonCardProps = {
  title: string;
  children: ReactNode;
  className?: string;
};

export default function JsonCard({ title, children, className }: JsonCardProps) {
  return (
    <section className={cn("rounded-xl border border-slate-200 bg-white p-4 shadow-sm", className)}>
      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-600">{title}</h2>
      <div className="space-y-2 text-sm text-slate-800">{children}</div>
    </section>
  );
}
