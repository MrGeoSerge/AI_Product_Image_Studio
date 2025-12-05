import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';

type SkinTone = 'light' | 'medium' | 'dark';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="card" *ngIf="product">
      <div class="product-detail-header">
        <div>
          <h2>{{ product.name }}</h2>
          <p class="product-detail-subtitle">
            Color: <strong>{{ product.color }}</strong> · Created {{ product.createdAt | date:'medium' }}
          </p>
        </div>
        <a routerLink="/" class="btn btn-secondary">Back to list</a>
      </div>

      <div class="product-detail-layout">
        <div class="product-detail-image-wrapper">
          <img
            [src]="currentImageUrl"
            [alt]="product.name"
            class="product-detail-image"
            (error)="onImageError($event)"
          />
        </div>

        <div class="product-detail-info">
          <h3>Skin tone preview</h3>
          <p class="product-detail-description">
            Preview how this product looks on people with different skin tones. Select an option below.
          </p>

          <div class="tone-selector">
            <button
              class="tone-chip light"
              [class.active]="selectedTone === 'light'"
              [disabled]="!product.imageUrlLight"
              (click)="setTone('light')"
            >
              Light
            </button>
            <button
              class="tone-chip medium"
              [class.active]="selectedTone === 'medium'"
              [disabled]="!product.imageUrlMedium"
              (click)="setTone('medium')"
            >
              Medium
            </button>
            <button
              class="tone-chip dark"
              [class.active]="selectedTone === 'dark'"
              [disabled]="!product.imageUrlDark"
              (click)="setTone('dark')"
            >
              Dark
            </button>
          </div>

          <div class="product-detail-description-block">
            <h3>Description</h3>
            <p>{{ product.description }}</p>
          </div>
        </div>
      </div>
    </div>

    <div *ngIf="loading" class="loading">
      Loading product...
    </div>

    <div *ngIf="error" class="error">
      {{ error }}
    </div>
  `,
  styles: [
    `
      .product-detail-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        margin-bottom: 24px;
      }

      .product-detail-subtitle {
        margin-top: 4px;
        color: #6c757d;
        font-size: 14px;
      }

      .product-detail-layout {
        display: grid;
        grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);
        gap: 32px;
        align-items: flex-start;
      }

      .product-detail-image-wrapper {
        background: radial-gradient(circle at top left, #ffffff, #f3f6ff);
        border-radius: 16px;
        padding: 24px;
        box-shadow: 0 14px 35px rgba(15, 23, 42, 0.18);
      }

      .product-detail-image {
        width: 100%;
        border-radius: 12px;
        object-fit: cover;
        max-height: 520px;
      }

      .product-detail-info h3 {
        margin-bottom: 8px;
      }

      .product-detail-description {
        font-size: 14px;
        color: #6c757d;
        margin-bottom: 16px;
      }

      .tone-selector {
        display: flex;
        gap: 12px;
        margin-bottom: 24px;
      }

      .tone-chip {
        padding: 8px 16px;
        border-radius: 999px;
        border: 1px solid transparent;
        cursor: pointer;
        font-size: 14px;
        font-weight: 500;
        background: #f1f3f5;
        color: #343a40;
        transition: all 0.2s ease;
      }

      .tone-chip.light {
        background: linear-gradient(90deg, #fbe9e7, #fff3e0);
      }

      .tone-chip.medium {
        background: linear-gradient(90deg, #ffe0b2, #ffcdd2);
      }

      .tone-chip.dark {
        background: linear-gradient(90deg, #d7ccc8, #a1887f);
        color: #fff;
      }

      .tone-chip.active {
        border-color: #007bff;
        box-shadow: 0 0 0 1px rgba(0, 123, 255, 0.4);
      }

      .tone-chip:disabled {
        opacity: 0.4;
        cursor: not-allowed;
        box-shadow: none;
      }

      .product-detail-description-block h3 {
        margin-bottom: 6px;
      }

      .product-detail-description-block p {
        font-size: 15px;
        line-height: 1.6;
      }

      @media (max-width: 900px) {
        .product-detail-layout {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class ProductDetailComponent implements OnInit {
  product: Product | null = null;
  loading = true;
  error: string | null = null;
  selectedTone: SkinTone = 'medium';
  currentImageUrl: string = 'https://via.placeholder.com/800x600?text=No+Image';

  constructor(private route: ActivatedRoute, private productService: ProductService) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error = 'Invalid product id';
      this.loading = false;
      return;
    }

    this.productService.getProductById(id).subscribe({
      next: (product) => {
        this.product = product;
        this.initToneAndImage();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load product: ' + err.message;
        this.loading = false;
      },
    });
  }

  private initToneAndImage(): void {
    if (!this.product) {
      return;
    }

    // Prefer medium, then light, then dark, then legacy image
    if (this.product.imageUrlMedium) {
      this.selectedTone = 'medium';
      this.currentImageUrl = this.product.imageUrlMedium;
    } else if (this.product.imageUrlLight) {
      this.selectedTone = 'light';
      this.currentImageUrl = this.product.imageUrlLight;
    } else if (this.product.imageUrlDark) {
      this.selectedTone = 'dark';
      this.currentImageUrl = this.product.imageUrlDark;
    } else if (this.product.imageUrl) {
      this.currentImageUrl = this.product.imageUrl;
    }
  }

  setTone(tone: SkinTone): void {
    if (!this.product) {
      return;
    }
    this.selectedTone = tone;
    switch (tone) {
      case 'light':
        this.currentImageUrl =
          this.product.imageUrlLight ||
          this.product.imageUrlMedium ||
          this.product.imageUrlDark ||
          this.product.imageUrl ||
          this.currentImageUrl;
        break;
      case 'medium':
        this.currentImageUrl =
          this.product.imageUrlMedium ||
          this.product.imageUrlLight ||
          this.product.imageUrlDark ||
          this.product.imageUrl ||
          this.currentImageUrl;
        break;
      case 'dark':
        this.currentImageUrl =
          this.product.imageUrlDark ||
          this.product.imageUrlMedium ||
          this.product.imageUrlLight ||
          this.product.imageUrl ||
          this.currentImageUrl;
        break;
    }
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.src = 'https://via.placeholder.com/800x600?text=No+Image';
    }
  }
}


