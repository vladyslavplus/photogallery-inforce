import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AlbumService } from '../../../core/services/album.service';
import { PhotoService } from '../../../core/services/photo.service';
import { AlbumResponseDto, AlbumCreateDto, AlbumUpdateDto, AlbumWithPhoto } from '../../../core/models/album.models';
import { PhotoResponseDto } from '../../../core/models/photo.models';

@Component({
  selector: 'app-my-albums',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './my-albums.html',
  styleUrl: './my-albums.css',
})
export class MyAlbums implements OnInit {
  albumsWithPhotos = signal<AlbumWithPhoto[]>([]);
  isLoadingAlbums = signal(false);
  errorMessage = signal<string | null>(null);
  
  currentPage = signal(1);
  pageSize = signal(9);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  
  showModal = signal(false);
  isEditMode = signal(false);
  isSubmitting = signal(false);
  albumForm = signal({ title: '', description: '' });
  editingAlbumId = signal<string | null>(null);
  
  showDeleteModal = signal(false);
  isDeleting = signal(false);
  albumToDelete = signal<AlbumResponseDto | null>(null);

  constructor(
    private albumService: AlbumService,
    private photoService: PhotoService
  ) {}

  ngOnInit(): void {
    this.loadAlbums();
  }

  loadAlbums(): void {
    this.isLoadingAlbums.set(true);
    this.errorMessage.set(null);

    this.albumService.getMyAlbums(this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        const albums = response.items.map(album => ({
          album,
          coverPhoto: null as PhotoResponseDto | null,
          isLoading: true
        }));

        this.albumsWithPhotos.set(albums);
        this.totalCount.set(response.totalCount);
        this.isLoadingAlbums.set(false);

        albums.forEach((item, index) => {
          this.loadCoverPhoto(index);
        });
      },
      error: (error) => {
        this.errorMessage.set('Failed to load albums');
        console.error('Error loading albums:', error);
        this.isLoadingAlbums.set(false);
      }
    });
  }

  private loadCoverPhoto(index: number): void {
    const album = this.albumsWithPhotos()[index];
    if (!album) return;

    this.photoService.getAlbumPhotos(album.album.id, 1, 1).subscribe({
      next: (response) => {
        const photo = response.items.length > 0 ? response.items[0] : null;

        this.albumsWithPhotos.update(albums => {
          const updated = [...albums];
          if (updated[index]) {
            updated[index] = {
              ...updated[index],
              coverPhoto: photo,
              isLoading: false
            };
          }
          return updated;
        });
      },
      error: (error) => {
        console.error(`Error loading photo for album:`, error);

        this.albumsWithPhotos.update(albums => {
          const updated = [...albums];
          if (updated[index]) {
            updated[index] = {
              ...updated[index],
              coverPhoto: null,
              isLoading: false
            };
          }
          return updated;
        });
      }
    });
  }

  getCoverPhotoUrl(item: AlbumWithPhoto): string | null {
    if (item.coverPhoto?.thumbnailUrl) {
      return item.coverPhoto.thumbnailUrl;
    }
    return item.album.coverPhotoUrl || null;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadAlbums();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    } else {
      if (current <= 4) {
        for (let i = 1; i <= 5; i++) pages.push(i);
        pages.push(-1);
        pages.push(total);
      } else if (current >= total - 3) {
        pages.push(1);
        pages.push(-1);
        for (let i = total - 4; i <= total; i++) pages.push(i);
      } else {
        pages.push(1);
        pages.push(-1);
        for (let i = current - 1; i <= current + 1; i++) pages.push(i);
        pages.push(-1);
        pages.push(total);
      }
    }

    return pages;
  }

  openCreateModal(): void {
    this.isEditMode.set(false);
    this.albumForm.set({ title: '', description: '' });
    this.editingAlbumId.set(null);
    this.showModal.set(true);
  }

  openEditModal(album: AlbumResponseDto): void {
    this.isEditMode.set(true);
    this.albumForm.set({
      title: album.title,
      description: album.description || ''
    });
    this.editingAlbumId.set(album.id);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.albumForm.set({ title: '', description: '' });
    this.editingAlbumId.set(null);
  }

  updateTitle(value: string): void {
    this.albumForm.update(form => ({ ...form, title: value }));
  }

  updateDescription(value: string): void {
    this.albumForm.update(form => ({ ...form, description: value }));
  }

  submitAlbum(): void {
    const form = this.albumForm();
    if (!form.title.trim()) {
      return;
    }

    this.isSubmitting.set(true);

    if (this.isEditMode()) {
      this.updateAlbum(form);
    } else {
      this.createAlbum(form);
    }
  }

  private createAlbum(form: { title: string; description: string }): void {
    const dto: AlbumCreateDto = {
      title: form.title,
      description: form.description || undefined
    };

    this.albumService.createAlbum(dto).subscribe({
      next: () => {
        this.currentPage.set(1);
        this.loadAlbums();
        this.closeModal();
        this.isSubmitting.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to create album');
        console.error('Error creating album:', error);
        this.isSubmitting.set(false);
      }
    });
  }

  private updateAlbum(form: { title: string; description: string }): void {
    const id = this.editingAlbumId();
    if (!id) return;

    const dto: AlbumUpdateDto = {
      title: form.title,
      description: form.description || undefined
    };

    this.albumService.updateAlbum(id, dto).subscribe({
      next: () => {
        this.loadAlbums();
        this.closeModal();
        this.isSubmitting.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to update album');
        console.error('Error updating album:', error);
        this.isSubmitting.set(false);
      }
    });
  }

  confirmDelete(album: AlbumResponseDto): void {
    this.albumToDelete.set(album);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    this.showDeleteModal.set(false);
    this.albumToDelete.set(null);
  }

  deleteAlbum(): void {
    const album = this.albumToDelete();
    if (!album) return;

    this.isDeleting.set(true);

    this.albumService.deleteAlbum(album.id).subscribe({
      next: () => {
        this.currentPage.set(1);
        this.loadAlbums();
        this.closeDeleteModal();
        this.isDeleting.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to delete album');
        console.error('Error deleting album:', error);
        this.isDeleting.set(false);
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}