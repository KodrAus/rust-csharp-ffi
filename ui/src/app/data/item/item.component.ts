import { Component, OnInit, Output, EventEmitter, ElementRef, ViewChild } from '@angular/core';

import { Item } from '../item';

@Component({
  selector: 'data-item',
  templateUrl: './item.component.html',
  styleUrls: ['./item.component.sass'],
  inputs: [
    'item'
  ]
})
export class ItemComponent implements OnInit {
  @Output() set = new EventEmitter<Item>();
  @Output() delete = new EventEmitter<Item>();
  @ViewChild('title') title: ElementRef; 

  private item: Item;

  constructor() { }

  ngOnInit() {
    if (this.item.isNew) {
      this.title.nativeElement.focus();
    }
  }

  onSet() {
    if (this.item.value.title !== '') {
      this.set.emit(this.item);
    }
  }

  onDelete() {
    this.delete.emit(this.item);
  }
}
