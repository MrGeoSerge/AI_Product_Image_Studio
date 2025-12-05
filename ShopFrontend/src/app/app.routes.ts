import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/product-list/product-list.component').then(m => m.ProductListComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./components/product-form/product-form.component').then(m => m.ProductFormComponent)
  },
  {
    path: 'product/:id',
    loadComponent: () => import('./components/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  }
];

