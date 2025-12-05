export interface Product {
  id: number;
  name: string;
  color: string;
  description: string;
  // Legacy single image url
  imageUrl?: string;
  // New: separate images for different skin tones
  imageUrlLight?: string;
  imageUrlMedium?: string;
  imageUrlDark?: string;
  createdAt: string;
  imageGenerationStatus: 'Pending' | 'Processing' | 'Completed' | 'Failed';
}

export interface CreateProductRequest {
  name: string;
  color: string;
  description: string;
}

export interface GenerateImageRequest {
  name: string;
  color: string;
  description: string;
}

export interface GenerateImageResponse {
  jobId: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  imageUrl?: string;
  error?: string;
}

