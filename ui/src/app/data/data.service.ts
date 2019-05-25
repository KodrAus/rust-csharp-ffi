import { Injectable } from '@angular/core';

import { Data } from './data';

@Injectable({
  providedIn: 'root'
})
export class DataService {
  constructor() { }

  getData(): Data[] {
    return [
      {
        id: "datadocs-1",
        title: "Test 1",
        description: "Some stuff"
      },
      {
        id: "datadocs-2",
        title: "Test 1",
        description: "Some stuff"
      }
    ]
  }

  setData(data: Data) {
    
  }
}
