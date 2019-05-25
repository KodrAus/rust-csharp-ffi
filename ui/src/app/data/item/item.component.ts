import { Component, OnInit } from '@angular/core';

import { Data } from '../data';

@Component({
  selector: 'data-item',
  templateUrl: './item.component.html',
  styleUrls: ['./item.component.sass'],
  inputs: [
    'value'
  ]
})
export class ItemComponent implements OnInit {
  private value: Data;

  constructor() { }

  ngOnInit() {
  }

}
