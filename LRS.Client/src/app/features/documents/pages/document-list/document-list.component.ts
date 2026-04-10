import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DocumentService, DocumentResponse } from '../../services/document.service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.css'
})
export class DocumentListComponent implements OnInit {
  sourceId: number | null = null;
  documents: DocumentResponse[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private documentService: DocumentService) { }

  ngOnInit(): void {
    // Get sourceId from query params or prompt user
    const urlParams = new URLSearchParams(window.location.search);
    const sourceIdParam = urlParams.get('sourceId');
    if (sourceIdParam) {
      this.sourceId = parseInt(sourceIdParam, 10);
      this.loadDocuments();
    }
  }

  loadDocuments(): void {
    if (!this.sourceId) {
      this.errorMessage = 'Please provide a Source ID.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    
    this.documentService.getDocumentsBySource(this.sourceId).subscribe({
      next: (docs) => {
        // Sort by submission date (newest first) to show latest version first
        this.documents = docs.sort((a, b) => {
          const dateA = a.submissionDate ? new Date(a.submissionDate).getTime() : 0;
          const dateB = b.submissionDate ? new Date(b.submissionDate).getTime() : 0;
          return dateB - dateA;
        });
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load documents', err);
        this.errorMessage = 'Failed to load documents. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onSourceIdChange(): void {
    if (this.sourceId) {
      this.loadDocuments();
    } else {
      this.documents = [];
    }
  }

  getLatestVersion(docs: DocumentResponse[]): DocumentResponse | null {
    // Get the latest non-void document (active version)
    const activeDocs = docs.filter(d => !d.isVoid);
    return activeDocs.length > 0 ? activeDocs[0] : null;
  }

  getVoidedVersions(docs: DocumentResponse[]): DocumentResponse[] {
    // Get all voided documents (previous versions)
    return docs.filter(d => d.isVoid).sort((a, b) => {
      const dateA = a.submissionDate ? new Date(a.submissionDate).getTime() : 0;
      const dateB = b.submissionDate ? new Date(b.submissionDate).getTime() : 0;
      return dateB - dateA;
    });
  }

  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString();
  }

  viewDocument(documentId: number, fileName: string): void {
    this.errorMessage = '';
    this.documentService.getDocumentInline(documentId).subscribe({
      next: (blob) => {
        const blobUrl = URL.createObjectURL(blob);
        const newWindow = window.open(blobUrl, '_blank', 'noopener');
        if (!newWindow) {
          this.errorMessage = 'Please allow pop-ups to view the document.';
          URL.revokeObjectURL(blobUrl);
          return;
        }
        newWindow.onload = () => URL.revokeObjectURL(blobUrl);
      },
      error: (err) => {
        console.error('Failed to open document', err);
        this.errorMessage = 'Failed to open document for viewing. Please try again.';
      }
    });
  }

  downloadDocument(documentId: number, fileName: string): void {
    this.errorMessage = '';
    this.documentService.downloadDocument(documentId).subscribe({
      next: ({ blob, fileName: downloadedFileName }) => {
        const resolvedName = fileName || downloadedFileName || 'document.pdf';
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = resolvedName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Failed to download document', err);
        this.errorMessage = 'Failed to download document. Please try again.';
      }
    });
  }
}

