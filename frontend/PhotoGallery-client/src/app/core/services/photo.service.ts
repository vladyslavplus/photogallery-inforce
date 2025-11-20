import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import {PhotoResponseDto, PhotoUploadDto,} from '../models/photo.models';
import { PagedResult } from '../models/common.models';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  private readonly apiUrl = `${environment.apiUrl}/photos`;

  constructor(private http: HttpClient) {}

  getAlbumPhotos(
    albumId: string,
    pageNumber: number = 1,
    pageSize: number = 5
  ): Observable<PagedResult<PhotoResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<PhotoResponseDto>>(
      `${this.apiUrl}/album/${albumId}`,
      { params }
    );
  }

  getPhotoById(id: string): Observable<PhotoResponseDto> {
    return this.http.get<PhotoResponseDto>(`${this.apiUrl}/${id}`);
  }

  uploadPhoto(dto: PhotoUploadDto): Observable<PhotoResponseDto> {
    const formData = new FormData();
    formData.append('albumId', dto.albumId);
    formData.append('title', dto.title || '');
    formData.append('file', dto.file);

    return this.http.post<PhotoResponseDto>(this.apiUrl, formData);
  }

  deletePhoto(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  likePhoto(id: string): Observable<PhotoResponseDto> {
    return this.http.post<PhotoResponseDto>(`${this.apiUrl}/${id}/like`, {});
  }

  dislikePhoto(id: string): Observable<PhotoResponseDto> {
    return this.http.post<PhotoResponseDto>(`${this.apiUrl}/${id}/dislike`, {});
  }
}