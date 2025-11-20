import { Routes } from '@angular/router';

export const ALBUMS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./albums-list/albums-list').then(m => m.AlbumsList)
  },
  {
    path: ':id',
    loadComponent: () => import('./album-detail/album-detail').then(m => m.AlbumDetail)
  }
];