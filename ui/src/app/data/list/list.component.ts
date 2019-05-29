import { Component, Input, Output, EventEmitter } from '@angular/core';

import { Item } from '../item';

@Component({
  selector: 'app-data-list',
  templateUrl: './list.component.html',
  styleUrls: ['./list.component.sass']
})
export class ListComponent {
  @Input() items: Item[] = [];
  @Output() set = new EventEmitter<Item>();
  @Output() delete = new EventEmitter<Item>();

  onSet(item: Item) {
    this.set.emit(item);
  }

  onDelete(item: Item) {
    this.delete.emit(item);
  }
}
