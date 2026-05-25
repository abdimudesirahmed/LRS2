import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { DocumentService, DocumentResponse } from '../../../features/documents/services/document.service';

@Component({
  selector: 'app-conflict-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './conflict-modal.component.html',
  styleUrls: ['./conflict-modal.component.css']
})
export class ConflictModalComponent implements OnInit {
  @Input() oldDoc!: DocumentResponse;
  @Input() newFileName!: string;
  @Input() newFileType!: string; // 'application/pdf' or 'image/png' etc
  @Input() newFileUrl!: string;  // preview blob url or data url
  @Input() appRegId!: string;
  @Input() uniqueParcelId!: string;
  @Input() adminSourceTypeEnglish!: string;
  @Input() createdBy!: string;
  
  // Passed object to return choice back to the caller
  @Input() result!: { choice: 'keep-old' | 'replace-new' | 'cancel' };

  @Output() closed = new EventEmitter<void>();

  oldFileUrl: SafeResourceUrl | null = null;
  oldFileType: string = 'application/pdf';
  isLoadingOldFile: boolean = false;
  oldFileError: string = '';

  sanitizedNewFileUrl: SafeResourceUrl | null = null;
  selectedOption: 'keep-old' | 'replace-new' = 'replace-new';

  constructor(
    private documentService: DocumentService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    if (this.newFileUrl) {
      this.sanitizedNewFileUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.newFileUrl);
    }
    this.loadOldDocument();
  }

  loadOldDocument(): void {
    if (!this.oldDoc || !this.oldDoc.id) return;
    
    this.isLoadingOldFile = true;
    this.oldFileError = '';

    this.documentService.getDocumentInline(this.oldDoc.id).subscribe({
      next: (blob) => {
        this.oldFileType = blob.type;
        const objectUrl = URL.createObjectURL(blob);
        this.oldFileUrl = this.sanitizer.bypassSecurityTrustResourceUrl(objectUrl);
        this.isLoadingOldFile = false;
      },
      error: (err) => {
        console.error('Failed to load existing document preview', err);
        this.oldFileError = 'Could not load preview for the existing document.';
        this.isLoadingOldFile = false;
      }
    });
  }

  selectOption(option: 'keep-old' | 'replace-new'): void {
    this.selectedOption = option;
  }

  confirm(): void {
    if (this.result) {
      this.result.choice = this.selectedOption;
    }
    this.closed.emit();
  }

  cancel(): void {
    if (this.result) {
      this.result.choice = 'cancel';
    }
    this.closed.emit();
  }
}
