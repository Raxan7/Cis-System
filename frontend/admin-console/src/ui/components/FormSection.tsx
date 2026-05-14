import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, type Resolver } from 'react-hook-form';
import type { ZodTypeAny } from 'zod';
import type { FieldDescriptor } from '../../lib/module-types';
import { ErrorCallout } from './ErrorCallout';

type FormSectionProps = {
  title: string;
  description?: string;
  fields: FieldDescriptor[];
  schema: ZodTypeAny;
  submitLabel?: string;
  onSubmit: (values: Record<string, unknown>) => Promise<void>;
  isSubmitting: boolean;
  error: unknown;
};

export function FormSection({
  title,
  description,
  fields,
  schema,
  submitLabel,
  onSubmit,
  isSubmitting,
  error,
}: FormSectionProps) {
  const defaultValues = Object.fromEntries(fields.map((field) => [field.name, field.type === 'checkbox' ? false : '']));
  const form = useForm<Record<string, unknown>>({
    resolver: zodResolver(schema as never) as Resolver<Record<string, unknown>>,
    defaultValues,
  });

  return (
    <section className="panel">
      <header className="panel__header">
        <div>
          <h3>{title}</h3>
          {description ? <p>{description}</p> : null}
        </div>
      </header>
      <ErrorCallout error={error} />
      <form
        className="form-grid"
        onSubmit={form.handleSubmit(async (values) => {
          await onSubmit(values);
          form.reset(defaultValues);
        })}
      >
        {fields.map((field) => {
          const registration = form.register(field.name);
          const fieldError = form.formState.errors[field.name]?.message as string | undefined;

          return (
            <label className={field.type === 'textarea' ? 'field field--full' : 'field'} key={field.name}>
              <span>{field.label}</span>
              {field.type === 'textarea' ? (
                <textarea rows={field.rows ?? 4} placeholder={field.placeholder} {...registration} />
              ) : field.type === 'select' ? (
                <select {...registration} defaultValue="">
                  <option value="">Select {field.label}</option>
                  {field.options?.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              ) : field.type === 'checkbox' ? (
                <input type="checkbox" {...registration} />
              ) : (
                <input
                  type={field.type === 'tags' ? 'text' : field.type}
                  placeholder={field.placeholder}
                  {...registration}
                />
              )}
              {field.helpText ? <small>{field.helpText}</small> : null}
              {fieldError ? <small className="field__error">{fieldError}</small> : null}
            </label>
          );
        })}
        <div className="form-actions">
          <button className="button button--primary" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Working...' : submitLabel ?? 'Submit'}
          </button>
        </div>
      </form>
    </section>
  );
}
