import { Component, OnInit } from '@angular/core';

import { DataService } from '../data.service';
import { Data } from '../data';
import { Item } from '../item';

@Component({
  selector: 'app-data',
  templateUrl: './data.component.html',
  styleUrls: ['./data.component.sass']
})
export class DataComponent implements OnInit {
  private data: Item[] = [];

  constructor(private dataService: DataService) { }

  ngOnInit() {
    this.getData();
  }

  getData() {
    this.dataService.getData()
    .subscribe((data: Data[]) => {
      this.data = data.map(value => ({
        isNew: false,
        isSaving: false,
        value
      }));
    });
  }

  createData() {
    this.data = [
      {
        isNew: true,
        isSaving: false,
        value: {
          id: this.dataService.nextId(),
          title: '',
          description: ''
        }
      },
      ...this.data
    ];
  }

  setData(item: Item) {
    item.isSaving = true;
    return this.dataService.setData(item.value).subscribe(() => {
      item.isSaving = false;
      item.isNew = false;
    });
  }

  deleteData(item: Item) {
    if (item.isNew) {
      this.data = this.data.filter(existing => existing.value.id !== item.value.id);
      return;
    }

    item.isSaving = true;
    return this.dataService.deleteData(item.value.id).subscribe(() => {
      this.data = this.data.filter(existing => existing.value.id !== item.value.id);
    });
  }
}
