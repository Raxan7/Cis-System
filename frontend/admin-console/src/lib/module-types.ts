import type { LucideIcon } from 'lucide-react';
import type { ZodTypeAny } from 'zod';

export type FieldType =
  | 'text'
  | 'email'
  | 'password'
  | 'textarea'
  | 'number'
  | 'select'
  | 'checkbox'
  | 'date'
  | 'datetime-local'
  | 'time'
  | 'tags';

export type FieldDescriptor = {
  name: string;
  label: string;
  type: FieldType;
  required?: boolean;
  placeholder?: string;
  helpText?: string;
  options?: { label: string; value: string }[];
  rows?: number;
};

export type ColumnDescriptor = {
  key: string;
  label: string;
  kind?: 'text' | 'status' | 'date' | 'datetime' | 'money' | 'json' | 'array';
};

export type RowAction = {
  label: string;
  permission?: string;
  confirmLabel?: string;
  body?: Record<string, unknown> | ((row: Record<string, unknown>, note: string) => Record<string, unknown> | undefined);
  run: (row: Record<string, unknown>, note: string) => Promise<unknown>;
};

export type SectionQuery = {
  key: string;
  title: string;
  description?: string;
  type: 'table' | 'record';
  fetch: (query: Record<string, string>) => Promise<Record<string, unknown>[] | Record<string, unknown>>;
  columns?: ColumnDescriptor[];
  queryFields?: FieldDescriptor[];
  defaultQuery?: Record<string, string>;
  requiredPermission?: string;
  entityType?: string | ((row: Record<string, unknown>) => string | undefined);
  entityId?: (row: Record<string, unknown>) => string | undefined;
  rowActions?: RowAction[];
};

export type SectionMutation = {
  key: string;
  title: string;
  description?: string;
  permission?: string;
  fields: FieldDescriptor[];
  schema: ZodTypeAny;
  submitLabel?: string;
  submit: (values: Record<string, unknown>) => Promise<unknown>;
};

export type ModuleDefinition = {
  route: string;
  title: string;
  description: string;
  icon: LucideIcon;
  navPermission?: string;
  featureLabel?: string;
  queries?: SectionQuery[];
  mutations?: SectionMutation[];
};
