import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

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

  checkDuplicate(parcelId: string, adminSourceTypeId: number): Observable<DocumentResponse | null> {
    return this.http.get<DocumentResponse | null>(`${this.apiUrl}/check-duplicate`, {
      params: { parcelId, adminSourceTypeId: adminSourceTypeId.toString() }
    });
  }

  getDocumentsBySource(sourceId: number): Observable<DocumentResponse[]> {
    return this.http.get<DocumentResponse[]>(`${this.apiUrl}/source/${sourceId}`);
  }

  getDocumentInline(documentId: number): Observable<Blob> {
    return this.http
      .get(`${this.apiUrl}/${documentId}`, {
        observe: 'response',
        responseType: 'blob'
      })
      .pipe(
        map((response) => {
          const contentType = response.headers.get('Content-Type') || 'application/pdf';
          const body = response.body ?? new Blob();
          return new Blob([body], { type: contentType });
        })
      );
  }

  downloadDocument(documentId: number): Observable<{ blob: Blob; fileName: string }> {
    return this.http
      .get(`${this.apiUrl}/${documentId}/download`, {
        observe: 'response',
        responseType: 'blob'
      })
      .pipe(
        map((response) => {
          const blob = response.body ?? new Blob();
          const contentDisposition = response.headers.get('Content-Disposition');
          const fileName = this.getFileNameFromContentDisposition(contentDisposition) || 'document.pdf';
          return { blob, fileName };
        })
      );
  }

  private getFileNameFromContentDisposition(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;
    const match = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
    if (match && match[1]) {
      return match[1].replace(/['"]/g, '');
    }
    return null;
  }
}
