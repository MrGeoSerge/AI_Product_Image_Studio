import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { ImageGenerationService } from '../../services/image-generation.service';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="card">
      <h2>Create new product</h2>

      <div *ngIf="error" class="error">
        {{ error }}
      </div>

      <div *ngIf="success" class="success">
        Product created successfully! Starting image generation for all skin tones...
      </div>

      <form [formGroup]="productForm" (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="name">Product name *</label>
          <input 
            id="name" 
            type="text" 
            formControlName="name" 
            placeholder="E.g., T-shirt"
            [class.error-input]="productForm.get('name')?.invalid && productForm.get('name')?.touched">
          <div *ngIf="productForm.get('name')?.invalid && productForm.get('name')?.touched" 
               style="color: #dc3545; font-size: 12px; margin-top: 5px;">
            Name is required
          </div>
        </div>

        <div class="form-group">
          <label for="color">Color *</label>
          <input 
            id="color" 
            type="text" 
            formControlName="color" 
            placeholder="E.g., blue"
            [class.error-input]="productForm.get('color')?.invalid && productForm.get('color')?.touched">
          <div *ngIf="productForm.get('color')?.invalid && productForm.get('color')?.touched" 
               style="color: #dc3545; font-size: 12px; margin-top: 5px;">
            Color is required
          </div>
        </div>

        <div class="form-group">
          <label for="description">Product description *</label>
          <textarea 
            id="description" 
            formControlName="description" 
            placeholder="Detailed description of the product..."
            [class.error-input]="productForm.get('description')?.invalid && productForm.get('description')?.touched">
          </textarea>
          <div *ngIf="productForm.get('description')?.invalid && productForm.get('description')?.touched" 
               style="color: #dc3545; font-size: 12px; margin-top: 5px;">
            Description is required
          </div>
        </div>

        <div style="display: flex; gap: 10px; margin-top: 20px;">
          <button 
            type="submit" 
            class="btn btn-primary"
            [disabled]="productForm.invalid || submitting">
            {{ submitting ? 'Creating...' : 'Create product and generate images' }}
          </button>
          
          <a routerLink="/" class="btn btn-secondary">Cancel</a>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .error-input {
      border-color: #dc3545 !important;
    }
  `]
})
export class ProductFormComponent {
  productForm: FormGroup;
  submitting = false;
  error: string | null = null;
  success = false;

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private imageGenerationService: ImageGenerationService,
    private router: Router
  ) {
    this.productForm = this.fb.group({
      name: ['', [Validators.required]],
      color: ['', [Validators.required]],
      description: ['', [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      return;
    }

    this.submitting = true;
    this.error = null;
    this.success = false;

    const productData = this.productForm.value;

    // First create the product
    this.productService.createProduct(productData).subscribe({
      next: (product) => {
        this.success = true;
        console.log('Product created:', product);

        // Then trigger image generation
        this.imageGenerationService.generateImageForProduct(product.id).subscribe({
          next: (response) => {
            console.log('Image generation started:', response);
            
            // Navigate back to list after short delay
            setTimeout(() => {
              this.router.navigate(['/']);
            }, 2000);
          },
          error: (err) => {
            console.error('Image generation error:', err);
            // Product is created but generation failed to start
            // Navigate back to list anyway
            setTimeout(() => {
              this.router.navigate(['/']);
            }, 2000);
          }
        });
      },
      error: (err) => {
        this.error = 'Failed to create product: ' + err.message;
        this.submitting = false;
        console.error(err);
      }
    });
  }
}

