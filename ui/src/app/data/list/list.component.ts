import { Component, OnInit } from '@angular/core';

import { Data } from '../data';

@Component({
  selector: 'data-list',
  templateUrl: './list.component.html',
  styleUrls: ['./list.component.sass'],
  inputs: [
    'items'
  ]
})
export class ListComponent implements OnInit {
  private items: Data[] = [];

  constructor() { }

  ngOnInit() {
  }

}
