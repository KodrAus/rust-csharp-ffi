import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { HttpClient } from '@angular/common/http';

import { environment } from '../../environments/environment';
import { Data } from './data';

interface Value {
  title: string;
  description: string;
}

type Get = [
  {
    key: string,
    value: Value
  }
];

type Set = Value;

function getRandomInt(min, max): number {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}

@Injectable({
  providedIn: 'root'
})
export class DataService {
  constructor(private http: HttpClient) { }

  nextId(): string {
    return `quickdoc-${getRandomInt(0, 18_446_744_073_709_551_615)}`;
  }

  getData(): Observable<Data[]> {
    return this.http
      .get<Get>(`${environment.api}/api/data`, {
        headers: {
          'content-type': 'application/json'
        }
      })
      .pipe(map(res => res
        .map(data => ({
          id: data.key,
          title: data.value.title,
          description: data.value.description
        }))));
  }

  setData(data: Data): Observable<any> {
    const body: Set = {
      title: data.title,
      description: data.description
    };

    return this.http.post(`${environment.api}/api/data/${data.id}`, body, {
      headers: {
        'content-type': 'application/json'
      }
    });
  }

  deleteData(key: string): Observable<any> {
    return this.http.delete(`${environment.api}/api/data/${key}`);
  }
}
