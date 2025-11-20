import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AlbumService } from '../../../core/services/album.service';
import { PhotoService } from '../../../core/services/photo.service';
import { AuthService } from '../../../core/services/auth.service';
import { AlbumResponseDto, AlbumWithPhoto } from '../../../core/models/album.models';
import { PhotoResponseDto } from '../../../core/models/photo.models';

@Component({
  selector: 'app-albums-list',
  imports: [CommonModule, RouterLink],
  templateUrl: './albums-list.html',
  styleUrl: './albums-list.css',
})
export class AlbumsList implements OnInit {
  albums = signal<AlbumWithPhoto[]>([]);
  isLoadingAlbums = signal(false);
  errorMessage = signal<string | null>(null);
  
  currentPage = signal(1);
  pageSize = signal(5);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  
  isDeleting = signal<string | null>(null);
  albumToDelete = signal<AlbumResponseDto | null>(null);
  showDeleteModal = signal(false);

  constructor(
    private albumService: AlbumService,
    private photoService: PhotoService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadAlbums();
  }

  loadAlbums(): void {
    this.isLoadingAlbums.set(true);
    this.errorMessage.set(null);

    this.albumService.getAllAlbums(this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        const albumsWithCovers = response.items.map(album => ({
          album,
          coverPhoto: null as PhotoResponseDto | null,
          isLoading: true
        }));

        this.albums.set(albumsWithCovers);
        this.totalCount.set(response.totalCount);
        this.isLoadingAlbums.set(false);

        albumsWithCovers.forEach((item, index) => {
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
    const item = this.albums()[index];
    if (!item) return;

    this.photoService.getAlbumPhotos(item.album.id, 1, 1).subscribe({
      next: (response) => {
        const photo = response.items.length > 0 ? response.items[0] : null;

        this.albums.update(albums => {
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
        console.error(`Error loading cover photo:`, error);

        this.albums.update(albums => {
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

    this.isDeleting.set(album.id);

    this.albumService.deleteAlbum(album.id).subscribe({
      next: () => {
        this.albums.update(albums =>
          albums.filter(a => a.album.id !== album.id)
        );
        this.totalCount.update(count => count - 1);
        this.closeDeleteModal();
        this.isDeleting.set(null);
      },
      error: (error) => {
        console.error('Error deleting album:', error);
        this.errorMessage.set('Failed to delete album');
        this.isDeleting.set(null);
      }
    });
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

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}