import { Component, ElementRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../services/document.service';
import { AdministrativeSourceTypeService, AdministrativeSourceType } from '../../services/administrative-source-type.service';

@Component({
  selector: 'app-scan',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './scan.component.html',
  styleUrl: './scan.component.css'
})
export class ScanComponent implements OnInit {
  @ViewChild('video') videoElement!: ElementRef<HTMLVideoElement>;
  @ViewChild('canvas') canvasElement!: ElementRef<HTMLCanvasElement>;

  appRegId: string = '';
  // uniqueParcelId: string = ''; // Removed
  adminSourceTypeId: number = 0;
  sourceId: number | null = null;
  createdBy: string = '';

  // Load administrative source types from API (SRS requirement)
  administrativeSourceTypes: AdministrativeSourceType[] = [];
  isLoadingTypes: boolean = false;

  isScanning: boolean = false;
  hasCapturedImage: boolean = false;
  capturedImageSrc: string | null = null;
  uploadedFile: File | null = null;

  statusMessage: string = '';
  isUploading: boolean = false;

  // Form validation
  formErrors: { [key: string]: string } = {};

  constructor(
    private documentService: DocumentService,
    private adminSourceTypeService: AdministrativeSourceTypeService
  ) { }

  ngOnInit(): void {
    this.loadAdministrativeSourceTypes();
  }

  loadAdministrativeSourceTypes(): void {
    this.isLoadingTypes = true;
    this.adminSourceTypeService.getAll().subscribe({
      next: (types) => {
        this.administrativeSourceTypes = types;
        if (types.length > 0) {
          this.adminSourceTypeId = types[0].id; // Default to first type
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
    this.isScanning = true;
    this.hasCapturedImage = false;
    this.statusMessage = 'Initializing scanner (camera)...';

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: true });
      this.videoElement.nativeElement.srcObject = stream;
      this.videoElement.nativeElement.play();
      this.statusMessage = 'Ready to scan. Align document and click Capture.';
    } catch (err) {
      console.error('Error accessing camera:', err);
      this.statusMessage = 'Error: Could not access camera/scanner. Please ensure permissions are granted.';
      this.isScanning = false;
    }
  }

  captureImage() {
    const video = this.videoElement.nativeElement;
    const canvas = this.canvasElement.nativeElement;

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    const context = canvas.getContext('2d');
    if (context) {
      context.drawImage(video, 0, 0, canvas.width, canvas.height);

      // Convert to Blob/File
      canvas.toBlob(blob => {
        if (blob) {
          const fileName = `Scan_${new Date().getTime()}.png`;
          this.uploadedFile = new File([blob], fileName, { type: 'image/png' });
          this.capturedImageSrc = URL.createObjectURL(blob);
          this.hasCapturedImage = true;
          this.stopCamera();
          this.statusMessage = 'Document captured. Review and Upload.';
        }
      }, 'image/png');
    }
  }

  stopCamera() {
    const stream = this.videoElement.nativeElement.srcObject as MediaStream;
    if (stream) {
      stream.getTracks().forEach(track => track.stop());
    }
    this.isScanning = false;
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.uploadedFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => this.capturedImageSrc = e.target.result;
      reader.readAsDataURL(file);
      this.hasCapturedImage = true;
      this.statusMessage = 'File selected. Ready to upload.';
    }
  }

  validateForm(): boolean {
    this.formErrors = {};

    if (!this.uploadedFile) {
      this.formErrors['file'] = 'Please scan or select a document to upload.';
    }

    if (!this.appRegId || this.appRegId.trim() === '') {
      this.formErrors['appRegId'] = 'Application Registration ID is required.';
    }

    // if (!this.uniqueParcelId || this.uniqueParcelId.trim() === '') {
    //   this.formErrors['uniqueParcelId'] = 'Unique Parcel ID is required.';
    // }

    if (!this.adminSourceTypeId || this.adminSourceTypeId === 0) {
      this.formErrors['adminSourceTypeId'] = 'Administrative Source Type is required.';
    }

    return Object.keys(this.formErrors).length === 0;
  }

  uploadDocument(): void {
    // SRS User Story 1: Metadata form is mandatory
    if (!this.validateForm()) {
      this.statusMessage = 'Please fill in all required fields.';
      return;
    }

    if (!this.uploadedFile) {
      this.statusMessage = 'No document to upload.';
      return;
    }

    this.isUploading = true;
    this.statusMessage = 'Uploading document...';

    const formData = new FormData();
    formData.append('File', this.uploadedFile);
    formData.append('AppRegId', this.appRegId.trim());
    // formData.append('UniqueParcelId', this.uniqueParcelId.trim());
    formData.append('AdministrativeSourceTypeId', this.adminSourceTypeId.toString());

    if (this.sourceId) {
      formData.append('SourceId', this.sourceId.toString());
    }

    if (this.createdBy) {
      formData.append('CreatedBy', this.createdBy.trim());
    }

    this.documentService.uploadDocument(formData).subscribe({
      next: (response) => {
        console.log('Upload success', response);
        this.statusMessage = `Success! Document uploaded. Document ID: ${response.id}`;
        this.isUploading = false;
        this.resetForm();

        // Show success message for 3 seconds, then clear
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
            // Validation errors from API
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
          errorMessage += 'Unknown error. Please check console for details.';
        }
        this.statusMessage = errorMessage;
      }
    });
  }

  resetForm(): void {
    this.hasCapturedImage = false;
    this.capturedImageSrc = null;
    this.uploadedFile = null;
    this.formErrors = {};
    // Keep form fields for convenience (user might upload multiple documents)
  }
}
