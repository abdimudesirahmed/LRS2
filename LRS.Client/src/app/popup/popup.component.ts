import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalService } from '../shared/modal/modal.service';
import { DocumentService } from '../features/documents/services/document.service';
import { AdministrativeSourceTypeService, AdministrativeSourceType } from '../features/documents/services/administrative-source-type.service';

@Component({
  selector: 'app-popup-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './popup.component.html',
  styleUrls: ['./popup.component.css']
})
export class PopupComponent implements OnInit {
  adminSourceTypeId: number = 0;
  appRegId: string = '';
  uniqueParcelId: string = '';
  sourceId: number | null = null;
  createdBy: string = '';

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
          this.adminSourceTypeId = types[0].id; // default to the first type
        }
        this.isLoadingTypes = false;
      },
      error: (err) => {
        console.error('Failed to load administrative source types', err);
        this.statusMessage = 'Failed to load document types. Please refresh the page.';
        this.isLoadingTypes = false;
      }
    });
  }

  async startScan() {
    if (!this.appRegId || this.appRegId.trim() === '') {
      this.formErrors['appRegId'] = 'Application Registration ID is required before scanning.';
      this.statusMessage = 'Please enter an Application Registration ID first.';
      return;
    }
    if (!this.uniqueParcelId || this.uniqueParcelId.trim() === '') {
      this.formErrors['uniqueParcelId'] = 'Unique Parcel ID is required before scanning.';
      this.statusMessage = 'Please enter a Unique Parcel ID first.';
      return;
    }
    if (!this.adminSourceTypeId || this.adminSourceTypeId === 0) {
      this.formErrors['adminSourceTypeId'] = 'Please select an Administrative Source Type first.';
      this.statusMessage = 'Please select a document type first.';
      return;
    }

    this.formErrors = {};
    this.statusMessage = '';

    // Lazy-load scan component and open in modal as a popup
    const mod = await import('../features/scanning/pages/scan/scan.component');
    
    await this.modal.openComponent(
      mod.ScanComponent,
      { title: 'Document Scanner Workbench' },
      {
        appRegId: this.appRegId,
        uniqueParcelId: this.uniqueParcelId,
        adminSourceTypeId: this.adminSourceTypeId,
        sourceId: this.sourceId,
        createdBy: this.createdBy
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

    if (!this.appRegId || this.appRegId.trim() === '') {
      this.formErrors['appRegId'] = 'Application Registration ID is required.';
    }

    if (!this.uniqueParcelId || this.uniqueParcelId.trim() === '') {
      this.formErrors['uniqueParcelId'] = 'Unique Parcel ID is required.';
    }

    if (!this.adminSourceTypeId || this.adminSourceTypeId === 0) {
      this.formErrors['adminSourceTypeId'] = 'Administrative Source Type is required.';
    }

    if (!this.uploadedFile) {
      this.formErrors['file'] = 'Please select a file to upload or use the scanner.';
    }

    return Object.keys(this.formErrors).length === 0;
  }

  uploadDocument() {
    if (!this.validateForm()) {
      this.statusMessage = 'Please fill in all required fields.';
      return;
    }

    if (!this.uploadedFile) {
      this.statusMessage = 'No file chosen to upload.';
      return;
    }

    this.isUploading = true;
    this.statusMessage = 'Uploading document...';

    const formData = new FormData();
    formData.append('File', this.uploadedFile);
    formData.append('AppRegId', this.appRegId.trim());
    formData.append('UniqueParcelId', this.uniqueParcelId.trim());
    
    if (this.sourceId) {
      formData.append('SourceId', this.sourceId.toString());
    }
    if (this.createdBy) {
      formData.append('CreatedBy', this.createdBy.trim());
    }
    formData.append('AdministrativeSourceTypeId', this.adminSourceTypeId.toString());

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
    // Keep appRegId / sourceId / createdBy for convenience
  }
}
