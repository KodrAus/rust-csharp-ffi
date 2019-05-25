import { Component, OnInit } from '@angular/core';

import { DataService } from '../data.service';
import { Data } from '../data';

@Component({
  selector: 'app-data',
  templateUrl: './data.component.html',
  styleUrls: ['./data.component.sass']
})
export class DataComponent implements OnInit {
  private data: Data[] = [];

  constructor(private dataService: DataService) { }

  ngOnInit() {
    this.getData();
  }

  getData() {
    this.data = this.dataService.getData();
  }
}
