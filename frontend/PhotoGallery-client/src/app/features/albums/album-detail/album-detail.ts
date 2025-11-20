import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AlbumService } from '../../../core/services/album.service';
import { PhotoService } from '../../../core/services/photo.service';
import { AuthService } from '../../../core/services/auth.service';
import { AlbumResponseDto } from '../../../core/models/album.models';
import { PhotoResponseDto } from '../../../core/models/photo.models';

@Component({
  selector: 'app-album-detail',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './album-detail.html',
  styleUrl: './album-detail.css',
})
export class AlbumDetail implements OnInit {
  albumId = signal<string | null>(null);
  album = signal<AlbumResponseDto | null>(null);
  photos = signal<PhotoResponseDto[]>([]);
  
  isLoadingAlbum = signal(false);
  isLoadingPhotos = signal(false);
  errorMessage = signal<string | null>(null);
  
  currentPage = signal(1);
  pageSize = signal(5);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  
  selectedPhoto = signal<PhotoResponseDto | null>(null);
  showPhotoModal = signal(false);
  isDeletingPhoto = signal(false);
  isLikingPhoto = signal<string | null>(null);
  
  showUploadModal = signal(false);
  isUploading = signal(false);
  uploadTitle = signal('');
  selectedFile = signal<File | null>(null);
  uploadError = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    private albumService: AlbumService,
    private photoService: PhotoService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.albumId.set(params['id']);
      if (params['id']) {
        this.loadAlbumAndPhotos();
      }
    });
  }

  loadAlbumAndPhotos(): void {
    this.loadAlbum();
    this.loadPhotos();
  }

  private loadAlbum(): void {
    const id = this.albumId();
    if (!id) return;

    this.isLoadingAlbum.set(true);
    this.albumService.getAlbumById(id).subscribe({
      next: (album) => {
        this.album.set(album);
        this.isLoadingAlbum.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load album');
        console.error('Error loading album:', error);
        this.isLoadingAlbum.set(false);
      }
    });
  }

  loadPhotos(): void {
    const id = this.albumId();
    if (!id) return;

    this.isLoadingPhotos.set(true);
    this.errorMessage.set(null);

    this.photoService.getAlbumPhotos(id, this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        this.photos.set(response.items);
        this.totalCount.set(response.totalCount);
        this.album.update(a => a ? { ...a, photosCount: response.totalCount } : a);
        this.isLoadingPhotos.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load photos');
        console.error('Error loading photos:', error);
        this.isLoadingPhotos.set(false);
      }
    });
  }

  isAlbumOwner(): boolean {
    const album = this.album();
    if (!album) return false;
    return this.authService.isOwner(album.userId);
  }

  canUploadPhoto(): boolean {
    if (!this.authService.isAuthenticated()) return false;
    return this.isAlbumOwner() || this.authService.isAdmin();
  }

  canDeletePhoto(): boolean {
    if (!this.authService.isAuthenticated()) return false;
    return this.isAlbumOwner() || this.authService.isAdmin();
  }

  canEditAlbum(): boolean {
    if (!this.authService.isAuthenticated()) return false;
    return this.isAlbumOwner() || this.authService.isAdmin();
  }

  canDeleteAlbum(): boolean {
    if (!this.authService.isAuthenticated()) return false;
    return this.isAlbumOwner() || this.authService.isAdmin();
  }

  openPhotoModal(photo: PhotoResponseDto): void {
    this.selectedPhoto.set(photo);
    this.showPhotoModal.set(true);
  }

  closePhotoModal(): void {
    this.showPhotoModal.set(false);
    this.selectedPhoto.set(null);
  }

  likePhoto(photo: PhotoResponseDto): void {
    if (!this.authService.isAuthenticated()) return;

    this.isLikingPhoto.set(photo.id);

    this.photoService.likePhoto(photo.id).subscribe({
      next: (updatedPhoto) => {
        this.updatePhotoInList(updatedPhoto);
        this.isLikingPhoto.set(null);
      },
      error: (error) => {
        console.error('Error liking photo:', error);
        this.isLikingPhoto.set(null);
      }
    });
  }

  dislikePhoto(photo: PhotoResponseDto): void {
    if (!this.authService.isAuthenticated()) return;

    this.isLikingPhoto.set(photo.id);

    this.photoService.dislikePhoto(photo.id).subscribe({
      next: (updatedPhoto) => {
        this.updatePhotoInList(updatedPhoto);
        this.isLikingPhoto.set(null);
      },
      error: (error) => {
        console.error('Error disliking photo:', error);
        this.isLikingPhoto.set(null);
      }
    });
  }

  deletePhoto(photo: PhotoResponseDto): void {
    if (!this.canDeletePhoto()) {
      this.errorMessage.set('You do not have permission to delete this photo');
      return;
    }

    if (!confirm(`Are you sure you want to delete this photo?`)) {
      return;
    }

    this.isDeletingPhoto.set(true);

    this.photoService.deletePhoto(photo.id).subscribe({
      next: () => {
        this.photos.update(photos => 
          photos.filter(p => p.id !== photo.id)
        );
        this.totalCount.update(count => count - 1);
        this.album.update(a => a ? { ...a, photosCount: Math.max(0, a.photosCount - 1) } : a);
        this.isDeletingPhoto.set(false);
      },
      error: (error) => {
        console.error('Error deleting photo:', error);
        this.errorMessage.set('Failed to delete photo');
        this.isDeletingPhoto.set(false);
      }
    });
  }

  private updatePhotoInList(updatedPhoto: PhotoResponseDto): void {
    this.photos.update(photos =>
      photos.map(p => p.id === updatedPhoto.id ? updatedPhoto : p)
    );

    if (this.selectedPhoto()?.id === updatedPhoto.id) {
      this.selectedPhoto.set(updatedPhoto);
    }
  }

  getUserLikeStatus(photo: PhotoResponseDto): 'liked' | 'disliked' | null {
    if (photo.currentUserLiked === true) return 'liked';
    if (photo.currentUserLiked === false) return 'disliked';
    return null;
  }

  openUploadModal(): void {
    if (!this.canUploadPhoto()) {
      this.errorMessage.set('Only album owner or admin can upload photos');
      return;
    }

    this.showUploadModal.set(true);
    this.uploadTitle.set('');
    this.selectedFile.set(null);
    this.uploadError.set(null);
  }

  closeUploadModal(): void {
    this.showUploadModal.set(false);
    this.uploadTitle.set('');
    this.selectedFile.set(null);
    this.uploadError.set(null);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      if (!file.type.startsWith('image/')) {
        this.uploadError.set('Please select a valid image file');
        return;
      }

      const maxSize = 10 * 1024 * 1024;
      if (file.size > maxSize) {
        this.uploadError.set('File size must be less than 10MB');
        return;
      }

      this.selectedFile.set(file);
      this.uploadError.set(null);
    }
  }

  uploadPhoto(): void {
    if (!this.canUploadPhoto()) {
      this.uploadError.set('You do not have permission to upload photos to this album');
      return;
    }

    const file = this.selectedFile();
    const albumId = this.albumId();

    if (!file || !albumId) {
      this.uploadError.set('Please select a file');
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);

    this.photoService.uploadPhoto({
      albumId,
      title: this.uploadTitle() || undefined,
      file
    }).subscribe({
      next: (newPhoto) => {
        this.photos.update(photos => [newPhoto, ...photos]);
        this.totalCount.update(count => count + 1);
        this.album.update(a => a ? { ...a, photosCount: a.photosCount + 1 } : a);
        this.closeUploadModal();
        this.isUploading.set(false);
      },
      error: (error) => {
        console.error('Error uploading photo:', error);
        this.uploadError.set('Failed to upload photo');
        this.isUploading.set(false);
      }
    });
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadPhotos();
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