import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { AlbumCreateDto, AlbumUpdateDto, AlbumResponseDto } from '../models/album.models';
import { PagedResult } from '../models/common.models';

@Injectable({
  providedIn: 'root'
})
export class AlbumService {
  private readonly apiUrl = `${environment.apiUrl}/albums`;

  constructor(private http: HttpClient) {}

  getAllAlbums(pageNumber: number = 1, pageSize: number = 5): Observable<PagedResult<AlbumResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<AlbumResponseDto>>(this.apiUrl, { params });
  }

  getMyAlbums(pageNumber: number = 1, pageSize: number = 5): Observable<PagedResult<AlbumResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<AlbumResponseDto>>(`${this.apiUrl}/my`, { params });
  }

  getAlbumById(id: string): Observable<AlbumResponseDto> {
    return this.http.get<AlbumResponseDto>(`${this.apiUrl}/${id}`);
  }

  createAlbum(dto: AlbumCreateDto): Observable<AlbumResponseDto> {
    return this.http.post<AlbumResponseDto>(this.apiUrl, dto);
  }

  updateAlbum(id: string, dto: AlbumUpdateDto): Observable<AlbumResponseDto> {
    return this.http.put<AlbumResponseDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteAlbum(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}