import { PhotoResponseDto } from "./photo.models";

export interface AlbumCreateDto {
  title: string;
  description?: string;
}

export interface AlbumUpdateDto {
  title?: string;
  description?: string;
}

export interface AlbumResponseDto {
  id: string;
  title: string;
  description?: string;
  createdAt: string;
  userId: string;
  userName: string;
  photosCount: number;
  coverPhotoUrl?: string;
}

export interface AlbumWithPhoto {
  album: AlbumResponseDto;
  coverPhoto: PhotoResponseDto | null;
  isLoading: boolean;
}