import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MatCardModule } from '@angular/material/card';
import {MatGridListModule} from '@angular/material/grid-list';

import { ListComponent } from './list/list.component';
import { ItemComponent } from './item/item.component';
import { DataComponent } from './data/data.component';

@NgModule({
  declarations: [ListComponent, ItemComponent, DataComponent],
  imports: [
    CommonModule,
    MatCardModule,
    MatGridListModule
  ],
  exports: [
    DataComponent
  ]
})
export class DataModule { }
