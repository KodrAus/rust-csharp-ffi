import { Component, OnInit } from '@angular/core';

import { DataService } from '../data.service';
import { Document } from '../document';
import { Item } from '../item';

@Component({
  selector: 'app-data',
  templateUrl: './data.component.html',
  styleUrls: ['./data.component.sass']
})
export class DataComponent implements OnInit {
  data: Item[] = [];

  constructor(private dataService: DataService<Document>) { }

  ngOnInit() {
    this.getData();
  }

  getData() {
    this.dataService.getData()
    .subscribe(data => {
      this.data = data.map(doc => ({
        isNew: false,
        isSaving: false,
        key: doc.key,
        value: doc.value
      }));
    });
  }

  createData() {
    this.data = [
      {
        isNew: true,
        isSaving: false,
        key: this.dataService.nextKey(),
        value: {
          title: '',
          description: ''
        }
      },
      ...this.data
    ];
  }

  setData(item: Item) {
    item.isSaving = true;
    return this.dataService.setData({ key: item.key, value: item.value }).subscribe(() => {
      item.isSaving = false;
      item.isNew = false;
    });
  }

  deleteData(item: Item) {
    if (item.isNew) {
      this.data = this.data.filter(existing => existing.key !== item.key);
      return;
    }

    item.isSaving = true;
    return this.dataService.deleteData(item.key).subscribe(() => {
      this.data = this.data.filter(existing => existing.key !== item.key);
    });
  }
}
