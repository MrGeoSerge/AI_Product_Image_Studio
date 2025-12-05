import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { ImageGenerationService } from '../../services/image-generation.service';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
        <h2>Products</h2>
        <a routerLink="/create" class="btn btn-primary">+ Add product</a>
      </div>

      <div *ngIf="loading" class="loading">
        Loading...
      </div>

      <div *ngIf="error" class="error">
        {{ error }}
      </div>

      <div *ngIf="!loading && !error">
        <div *ngIf="products.length === 0" class="loading">
          No products yet. Create the first one!
        </div>

        <div class="grid" *ngIf="products.length > 0">
          <div 
            class="product-card clickable" 
            *ngFor="let product of products"
            (click)="openProduct(product.id)">
            <img 
              [src]="getPreviewImage(product)" 
              [alt]="product.name"
              class="product-image"
              (error)="onImageError($event)">
            
            <div class="product-info">
              <div class="product-title">{{ product.name }}</div>
              <div class="product-color">Color: {{ product.color }}</div>
              <div class="product-description">{{ product.description }}</div>
              
              <div style="margin-top: 10px;">
                <span class="status-badge" 
                      [ngClass]="{
                        'status-pending': product.imageGenerationStatus === 'Pending',
                        'status-processing': product.imageGenerationStatus === 'Processing',
                        'status-completed': product.imageGenerationStatus === 'Completed',
                        'status-failed': product.imageGenerationStatus === 'Failed'
                      }">
                  {{ getStatusText(product.imageGenerationStatus) }}
                </span>
              </div>

              <div style="margin-top: 15px; display: flex; gap: 10px; flex-wrap: wrap;">
                <button 
                  *ngIf="product.imageGenerationStatus !== 'Processing' && product.imageGenerationStatus !== 'Completed'"
                  class="btn btn-primary" 
                  (click)="generateImage(product.id); $event.stopPropagation()"
                  [disabled]="generating === product.id">
                  {{ generating === product.id ? 'Generating...' : 'Generate images' }}
                </button>
                
                <button 
                  class="btn btn-secondary" 
                  (click)="deleteProduct(product.id); $event.stopPropagation()"
                  [disabled]="deleting === product.id">
                  Delete
                </button>

                <a 
                  *ngIf="product.imageGenerationStatus === 'Completed'"
                  class="btn btn-outline"
                  [routerLink]="['/product', product.id]"
                  (click)="$event.stopPropagation()">
                  View details
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ProductListComponent implements OnInit {
  products: Product[] = [];
  loading = true;
  error: string | null = null;
  generating: number | null = null;
  deleting: number | null = null;

  constructor(
    private productService: ProductService,
    private imageGenerationService: ImageGenerationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.error = null;
    
    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Помилка завантаження товарів: ' + err.message;
        this.loading = false;
        console.error(err);
      }
    });
  }

  generateImage(productId: number): void {
    this.generating = productId;
    
    this.imageGenerationService.generateImageForProduct(productId).subscribe({
      next: (response) => {
        console.log('Генерація запущена:', response);
        // Оновлюємо список товарів
        setTimeout(() => {
          this.loadProducts();
          this.generating = null;
        }, 2000);
        
        // Можна додати polling для перевірки статусу
      },
      error: (err) => {
        this.error = 'Помилка генерації зображення: ' + err.message;
        this.generating = null;
        console.error(err);
      }
    });
  }

  deleteProduct(productId: number): void {
    if (!confirm('Are you sure you want to delete this product?')) {
      return;
    }

    this.deleting = productId;
    
    this.productService.deleteProduct(productId).subscribe({
      next: () => {
        this.loadProducts();
        this.deleting = null;
      },
      error: (err) => {
        this.error = 'Помилка видалення товару: ' + err.message;
        this.deleting = null;
        console.error(err);
      }
    });
  }

  getStatusText(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Pending': 'Pending',
      'Processing': 'Generating...',
      'Completed': 'Ready',
      'Failed': 'Failed'
    };
    return statusMap[status] || status;
  }

  getPreviewImage(product: Product): string {
    return (
      product.imageUrlMedium ||
      product.imageUrlLight ||
      product.imageUrlDark ||
      product.imageUrl ||
      'https://via.placeholder.com/300x200?text=No+Image'
    );
  }

  openProduct(productId: number): void {
    this.router.navigate(['/product', productId]);
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.src = 'https://via.placeholder.com/300x200?text=No+Image';
    }
  }
}

