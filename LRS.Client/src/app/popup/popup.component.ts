import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { FormlyModule, FormlyFieldConfig } from '@ngx-formly/core';
import { ModalService } from '../shared/modal/modal.service';
import { DocumentService } from '../features/documents/services/document.service';
import { AdministrativeSourceTypeService, AdministrativeSourceType } from '../features/documents/services/administrative-source-type.service';

@Component({
  selector: 'app-popup-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, FormlyModule],
  templateUrl: './popup.component.html',
  styleUrls: ['./popup.component.css']
})
export class PopupComponent implements OnInit {
  form = new FormGroup({});
  model: any = { adminSourceTypeId: 0, appRegId: '', uniqueParcelId: '', sourceId: null, createdBy: '' };
  fields: FormlyFieldConfig[] = [];

  administrativeSourceTypes: AdministrativeSourceType[] = [];
  isLoadingTypes: boolean = false;

  uploadedFile: File | null = null;
  capturedImageSrc: string | null = null;
  hasCapturedImage: boolean = false;

  isUploading: boolean = false;
  statusMessage: string = '';
  formErrors: Record<string, string> = {};

  constructor(
    private modal: ModalService,
    private documentService: DocumentService,
    private adminSourceTypeService: AdministrativeSourceTypeService
  ) {}

  ngOnInit(): void {
    this.loadAdministrativeSourceTypes();
  }

  loadAdministrativeSourceTypes(): void {
    this.isLoadingTypes = true;
    this.adminSourceTypeService.getAll().subscribe({
      next: (types) => {
        this.administrativeSourceTypes = types;
        if (types.length > 0) {
          this.model.adminSourceTypeId = types[0].id; // default to the first type
        }
        this.isLoadingTypes = false;
        this.initFormlyFields();
      },
      error: (err) => {
        console.error('Failed to load administrative source types', err);
        this.statusMessage = 'Failed to load document types. Please refresh the page.';
        this.isLoadingTypes = false;
      }
    });
  }

  initFormlyFields(): void {
    this.fields = [
      {
        key: 'adminSourceTypeId',
        type: 'select',
        props: {
          label: 'Administrative Source Type',
          required: true,
          placeholder: 'Select a document type...',
          options: this.administrativeSourceTypes.map(t => ({ label: t.englishValue, value: t.id })),
          disabled: this.isLoadingTypes || this.isUploading
        }
      },
      {
        fieldGroupClassName: 'form-row',
        fieldGroup: [
          {
            key: 'appRegId',
            type: 'input',
            props: {
              label: 'Application Registration ID',
              required: true,
              placeholder: 'e.g. appReg1000123',
              disabled: this.isUploading
            }
          },
          {
            key: 'uniqueParcelId',
            type: 'input',
            props: {
              label: 'Unique Parcel ID',
              required: true,
              placeholder: 'e.g. parcel-12345',
              disabled: this.isUploading
            }
          }
        ]
      },
      {
        fieldGroupClassName: 'form-row',
        fieldGroup: [
          {
            key: 'sourceId',
            type: 'input',
            props: {
              label: 'Source ID (Optional)',
              type: 'number',
              placeholder: 'New Source',
              disabled: this.isUploading
            }
          },
          {
            key: 'createdBy',
            type: 'input',
            props: {
              label: 'Created By (Optional)',
              placeholder: 'Your name or ID',
              disabled: this.isUploading
            }
          }
        ]
      }
    ];
  }

  updateFieldsDisabled(): void {
    this.initFormlyFields();
  }

  async startScan() {
    this.form.markAllAsTouched();
    const appRegId = this.model.appRegId;
    const uniqueParcelId = this.model.uniqueParcelId;
    const adminSourceTypeId = this.model.adminSourceTypeId;

    if (!appRegId || appRegId.trim() === '') {
      this.formErrors['appRegId'] = 'Application Registration ID is required before scanning.';
      this.statusMessage = 'Please enter an Application Registration ID first.';
      return;
    }
    if (!uniqueParcelId || uniqueParcelId.trim() === '') {
      this.formErrors['uniqueParcelId'] = 'Unique Parcel ID is required before scanning.';
      this.statusMessage = 'Please enter a Unique Parcel ID first.';
      return;
    }
    if (!adminSourceTypeId || adminSourceTypeId === 0) {
      this.formErrors['adminSourceTypeId'] = 'Please select an Administrative Source Type first.';
      this.statusMessage = 'Please select a document type first.';
      return;
    }

    this.formErrors = {};
    this.statusMessage = '';

    const mod = await import('../features/scanning/pages/scan/scan.component');
    
    await this.modal.openComponent(
      mod.ScanComponent,
      { title: 'Document Scanner Workbench' },
      {
        appRegId: appRegId.trim(),
        uniqueParcelId: uniqueParcelId.trim(),
        adminSourceTypeId: adminSourceTypeId,
        sourceId: this.model.sourceId,
        createdBy: this.model.createdBy?.trim() || ''
      }
    );
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.uploadedFile = file;
      this.hasCapturedImage = true;
      this.formErrors['file'] = '';

      if (file.type === 'application/pdf') {
        this.capturedImageSrc = null;
        this.statusMessage = 'PDF selected. Ready to upload.';
      } else {
        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.capturedImageSrc = String(e.target.result ?? '');
        };
        reader.readAsDataURL(file);
        this.statusMessage = 'File selected. Ready to upload.';
      }
    }
  }

  validateForm(): boolean {
    this.formErrors = {};
    this.form.markAllAsTouched();

    if (!this.uploadedFile) {
      this.formErrors['file'] = 'Please select a file to upload or use the scanner.';
    }

    return this.form.valid && !this.formErrors['file'];
  }

  async uploadDocument() {
    if (!this.validateForm()) {
      this.statusMessage = 'Please fill in all required fields.';
      return;
    }

    if (!this.uploadedFile) {
      this.statusMessage = 'No file chosen to upload.';
      return;
    }

    this.isUploading = true;
    this.updateFieldsDisabled();
    this.statusMessage = 'Checking for duplicate documents...';

    const uniqueParcelId = this.model.uniqueParcelId.trim();
    const adminSourceTypeId = this.model.adminSourceTypeId;

    try {
      const duplicate = await this.documentService.checkDuplicate(uniqueParcelId, adminSourceTypeId).toPromise();
      if (duplicate) {
        this.statusMessage = 'Duplicate found. Resolving conflict...';
        
        let newFileUrl = '';
        if (this.uploadedFile.type !== 'application/pdf') {
          newFileUrl = this.capturedImageSrc || '';
        } else {
          newFileUrl = URL.createObjectURL(this.uploadedFile);
        }

        const resultObj: { choice: 'keep-old' | 'replace-new' | 'cancel' } = { choice: 'cancel' };
        const mod = await import('../shared/components/conflict-modal/conflict-modal.component');
        
        await this.modal.openComponent(
          mod.ConflictModalComponent,
          { title: 'Document Conflict Detected' },
          {
            oldDoc: duplicate,
            newFileName: this.uploadedFile.name,
            newFileType: this.uploadedFile.type,
            newFileUrl: newFileUrl,
            appRegId: this.model.appRegId,
            uniqueParcelId: uniqueParcelId,
            adminSourceTypeEnglish: duplicate.adminSourceTypeEnglish || 'Selected Document Type',
            createdBy: this.model.createdBy || '',
            result: resultObj
          }
        );

        if (this.uploadedFile.type === 'application/pdf' && newFileUrl.startsWith('blob:')) {
          URL.revokeObjectURL(newFileUrl);
        }

        if (resultObj.choice === 'replace-new') {
          this.statusMessage = 'Replacing existing document...';
          this.executeUpload();
        } else if (resultObj.choice === 'keep-old') {
          this.statusMessage = 'Upload canceled. Kept the existing document.';
          this.isUploading = false;
          this.updateFieldsDisabled();
        } else {
          this.statusMessage = 'Upload canceled.';
          this.isUploading = false;
          this.updateFieldsDisabled();
        }
        return;
      }
    } catch (err) {
      console.error('Failed checking for duplicate documents', err);
    }

    this.executeUpload();
  }

  private executeUpload() {
    this.isUploading = true;
    this.updateFieldsDisabled();
    this.statusMessage = 'Uploading document...';

    const formData = new FormData();
    formData.append('File', this.uploadedFile!);
    formData.append('AppRegId', this.model.appRegId.trim());
    formData.append('UniqueParcelId', this.model.uniqueParcelId.trim());
    
    if (this.model.sourceId) {
      formData.append('SourceId', this.model.sourceId.toString());
    }
    if (this.model.createdBy) {
      formData.append('CreatedBy', this.model.createdBy.trim());
    }
    formData.append('AdministrativeSourceTypeId', this.model.adminSourceTypeId.toString());

    this.documentService.uploadDocument(formData).subscribe({
      next: (response) => {
        console.log('Upload success', response);
        this.statusMessage = `Success! Document uploaded. Document ID: ${response.id}`;
        this.isUploading = false;
        this.resetForm();

        setTimeout(() => {
          if (this.statusMessage.includes('Success!')) {
            this.statusMessage = '';
          }
        }, 3000);
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.isUploading = false;
        this.updateFieldsDisabled();

        let errorMessage = 'Upload failed. ';
        if (err.error) {
          if (err.error.errors && Array.isArray(err.error.errors)) {
            errorMessage = 'Validation errors: ' + err.error.errors.join(', ');
          } else if (typeof err.error === 'string') {
            errorMessage += err.error;
          } else if (err.error.message) {
            errorMessage += err.error.message;
          } else {
            errorMessage += JSON.stringify(err.error);
          }
        } else if (err.message) {
          errorMessage += err.message;
        } else {
          errorMessage += 'Unknown error. Please check console.';
        }
        this.statusMessage = errorMessage;
      }
    });
  }

  resetForm(): void {
    this.uploadedFile = null;
    this.capturedImageSrc = null;
    this.hasCapturedImage = false;
    this.formErrors = {};
    
    const currentValues = { ...this.model };
    this.form.reset(currentValues);
    this.updateFieldsDisabled();
  }
}
