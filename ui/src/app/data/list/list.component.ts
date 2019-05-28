import { Component, OnInit, Output, EventEmitter } from '@angular/core';

import { Item } from '../item';

@Component({
  selector: 'data-list',
  templateUrl: './list.component.html',
  styleUrls: ['./list.component.sass'],
  inputs: [
    'items'
  ]
})
export class ListComponent implements OnInit {
  @Output() set = new EventEmitter<Item>();
  @Output() delete = new EventEmitter<Item>();

  items: Item[] = [];

  constructor() { }

  ngOnInit() {
  }

  onSet(item: Item) {
    this.set.emit(item);
  }

  onDelete(item: Item) {
    this.delete.emit(item);
  }
}
