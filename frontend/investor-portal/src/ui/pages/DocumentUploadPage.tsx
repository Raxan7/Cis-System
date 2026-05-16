import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { uploadPortalDocument } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  documentType: z.string().trim().min(3, 'Document type is required.').max(100),
});

const allowedExtensions = ['pdf', 'png', 'jpg', 'jpeg'];
const allowedMimeTypes = ['application/pdf', 'image/png', 'image/jpeg'];
const maxSizeBytes = 10 * 1024 * 1024;

type FormValues = z.infer<typeof schema>;

export function DocumentUploadPage() {
  const navigate = useNavigate();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      documentType: 'KYC',
    },
  });
  const mutation = useMutation({
    mutationFn: uploadPortalDocument,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  const acceptedTypes = useMemo(() => allowedExtensions.map((extension) => `.${extension}`).join(','), []);

  function validateFile(file: File | null) {
    if (!file) {
      return 'Choose a file to continue.';
    }

    const extension = file.name.split('.').pop()?.toLowerCase();
    if (!extension || !allowedExtensions.includes(extension)) {
      return 'Only PDF and common image documents are accepted.';
    }

    if (!allowedMimeTypes.includes(file.type)) {
      return 'The selected file type is not allowed.';
    }

    if (file.size > maxSizeBytes) {
      return 'File size must be 10 MB or smaller.';
    }

    return null;
  }

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Document upload"
        title="Submit document metadata for review"
        description="This portal validates the selected file locally first, then submits the document metadata through the backend workflow for controlled processing."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Document details</h2>
          <p>Accepted formats: PDF, JPG, JPEG, PNG. Files larger than 10 MB are rejected before submission.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        {fileError ? <div className="callout callout--error"><div>{fileError}</div></div> : null}
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            const nextFileError = validateFile(selectedFile);
            setFileError(nextFileError);
            if (nextFileError || !selectedFile) {
              return;
            }

            const storageReference = `portal-upload/${new Date().getTime()}-${selectedFile.name}`;
            await mutation.mutateAsync({
              documentType: values.documentType,
              fileName: selectedFile.name,
              contentType: selectedFile.type,
              sizeBytes: selectedFile.size,
              storageReference,
            });
          })}
        >
          <label className="field">
            <span>Document type</span>
            <input {...form.register('documentType')} />
            <small className="field__error">{form.formState.errors.documentType?.message}</small>
          </label>
          <label className="field">
            <span>File</span>
            <input
              accept={acceptedTypes}
              type="file"
              onChange={(event) => {
                const file = event.target.files?.[0] ?? null;
                setSelectedFile(file);
                setFileError(validateFile(file));
              }}
            />
            <small>Selected files are validated locally before the metadata is sent to the backend.</small>
          </label>
          <div className="form-actions">
            <button className="button button--primary" disabled={mutation.isPending} type="submit">
              {mutation.isPending ? 'Submitting...' : 'Submit document'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
