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
    // Open document in new tab using inline viewer endpoint
    const url = `/api/documents/${documentId}`;
    window.open(url, '_blank');
  }

  downloadDocument(documentId: number, fileName: string): void {
    // Use download endpoint to force browser download
    const url = `/api/documents/${documentId}/download`;
    // Create a temporary anchor to navigate to the download URL
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName || 'document.pdf';
    document.body.appendChild(link);
    link.click();
    link.remove();
  }
}

