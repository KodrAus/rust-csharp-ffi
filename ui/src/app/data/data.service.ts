import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';

import { environment } from '../../environments/environment';
import { Data } from './data';

function getRandomInt(min, max): number {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}

@Injectable({
  providedIn: 'root'
})
export class DataService<T> {
  constructor(private http: HttpClient) { }

  nextKey(): string {
    return `quickdoc-${getRandomInt(0, 18_446_744_073_709_551_615)}`;
  }

  getData(): Observable<[Data<T>]> {
    return this.http
      .get<[Data<T>]>(`${environment.api}/api/data`, {
        headers: {
          'content-type': 'application/json'
        }
      });
  }

  setData(data: Data<T>): Observable<any> {
    return this.http.post(`${environment.api}/api/data/${data.key}`, data.value, {
      headers: {
        'content-type': 'application/json'
      }
    });
  }

  deleteData(key: string): Observable<any> {
    return this.http.delete(`${environment.api}/api/data/${key}`);
  }
}
