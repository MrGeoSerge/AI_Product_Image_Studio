import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GenerateImageRequest, GenerateImageResponse } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ImageGenerationService {
  private apiUrl = 'http://localhost:5000/api/imagegeneration';

  constructor(private http: HttpClient) { }

  generateImage(request: GenerateImageRequest): Observable<GenerateImageResponse> {
    return this.http.post<GenerateImageResponse>(`${this.apiUrl}/generate`, request);
  }

  generateImageForProduct(productId: number): Observable<GenerateImageResponse> {
    return this.http.post<GenerateImageResponse>(`${this.apiUrl}/generate-for-product/${productId}`, {});
  }

  getGenerationStatus(jobId: string): Observable<GenerateImageResponse> {
    return this.http.get<GenerateImageResponse>(`${this.apiUrl}/status/${jobId}`);
  }
}

