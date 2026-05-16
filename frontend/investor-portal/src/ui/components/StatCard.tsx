import type { LucideIcon } from 'lucide-react';

export function StatCard({
  title,
  value,
  caption,
  icon: Icon,
}: {
  title: string;
  value: string;
  caption: string;
  icon: LucideIcon;
}) {
  return (
    <section className="stat-card">
      <div className="stat-card__icon">
        <Icon size={18} />
      </div>
      <div>
        <p className="stat-card__label">{title}</p>
        <p className="stat-card__value">{value}</p>
        <p className="stat-card__caption">{caption}</p>
      </div>
    </section>
  );
}
