import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DocumentResponse {
  id: number;
  sourceId?: number;
  documentName?: string;
  submissionDate?: string;
  alfDocumentId?: string;
  appRegId: string;
  uniqueParcelId?: string; // Optional, removed from form input
  isVoid: boolean;
  adminSourceTypeEnglish?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = '/api/documents';

  constructor(private http: HttpClient) { }

  uploadDocument(formData: FormData): Observable<DocumentResponse> {
    return this.http.post<DocumentResponse>(`${this.apiUrl}/upload`, formData);
  }

  getDocumentsBySource(sourceId: number): Observable<DocumentResponse[]> {
    return this.http.get<DocumentResponse[]>(`${this.apiUrl}/source/${sourceId}`);
  }

  getDocumentFile(documentId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${documentId}`, { responseType: 'blob' });
  }

  downloadDocument(documentId: number, fileName: string): void {
    this.getDocumentFile(documentId).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName || 'document.pdf';
      link.click();
      window.URL.revokeObjectURL(url);
    });
  }
}
