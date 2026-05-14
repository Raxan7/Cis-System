import { z } from 'zod';
import type { FieldDescriptor } from './module-types';

function baseFieldSchema(field: FieldDescriptor) {
  switch (field.type) {
    case 'email':
      return z.string().trim().email('Enter a valid email address.');
    case 'number':
      return z.coerce.number().refine((value) => Number.isFinite(value), {
        message: `${field.label} must be a valid number.`,
      });
    case 'checkbox':
      return z.boolean();
    case 'tags':
      return z
        .string()
        .trim()
        .transform((value) => value.split(',').map((item) => item.trim()).filter(Boolean));
    default:
      return z.string().trim();
  }
}

export function schemaFromFields(fields: FieldDescriptor[]) {
  const shape = Object.fromEntries(
    fields.map((field) => {
      const schema = baseFieldSchema(field);

      if (field.required) {
        if (field.type === 'checkbox') {
          return [field.name, z.boolean()];
        }

        if (field.type === 'number') {
          return [
            field.name,
            (schema as z.ZodNumber).refine((value) => Number.isFinite(value), `${field.label} is required.`),
          ];
        }

        return [
          field.name,
          (schema as z.ZodTypeAny).refine((value) => {
            if (Array.isArray(value)) {
              return value.length > 0;
            }

            return value !== '' && value !== null && value !== undefined;
          }, `${field.label} is required.`),
        ];
      }

      return [
        field.name,
        (schema as z.ZodTypeAny).optional().transform((value) => {
          if (value === '') {
            return undefined;
          }

          return value;
        }),
      ];
    }),
  );

  return z.object(shape);
}

export function toRequestPayload(values: Record<string, unknown>, fields: FieldDescriptor[]) {
  return Object.fromEntries(
    fields.map((field) => {
      const value = values[field.name];

      if (field.type === 'datetime-local' && typeof value === 'string' && value) {
        return [field.name, new Date(value).toISOString()];
      }

      if (field.type === 'number' && value === undefined) {
        return [field.name, null];
      }

      return [field.name, value ?? null];
    }),
  );
}
