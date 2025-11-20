export interface PhotoResponseDto {
  id: string;
  fileName: string;
  photoUrl: string;
  thumbnailUrl: string;
  title: string;
  uploadedAt: string;
  albumId: string;
  albumTitle: string;
  likesCount: number;
  dislikesCount: number;
  currentUserLiked?: boolean;
}

export interface PhotoUploadDto {
  albumId: string;
  title?: string;
  file: File;
}