import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'albums',
    loadChildren: () => import('./features/albums/albums.routes').then(m => m.ALBUMS_ROUTES)
  },
  {
    path: 'my-albums',
    canActivate: [authGuard],
    loadComponent: () => import('./features/albums/my-albums/my-albums').then(m => m.MyAlbums)
  },
  {
    path: '',
    redirectTo: '/albums',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: '/albums'
  }
];