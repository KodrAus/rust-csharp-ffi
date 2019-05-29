import { Component, OnInit, Input, Output, EventEmitter, ElementRef, ViewChild } from '@angular/core';

import { Item } from '../item';

@Component({
  selector: 'app-data-item',
  templateUrl: './item.component.html',
  styleUrls: ['./item.component.sass'],
})
export class ItemComponent implements OnInit {
  @Input() item: Item;
  @Output() set = new EventEmitter<Item>();
  @Output() delete = new EventEmitter<Item>();
  @ViewChild('title') title: ElementRef;

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
