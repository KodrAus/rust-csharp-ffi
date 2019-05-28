import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { MatCardModule } from '@angular/material/card';
import {MatGridListModule} from '@angular/material/grid-list';
import {MatInputModule} from '@angular/material/input';
import {MatButtonModule} from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';

import { ListComponent } from './list/list.component';
import { ItemComponent } from './item/item.component';
import { DataComponent } from './data/data.component';

@NgModule({
  declarations: [ListComponent, ItemComponent, DataComponent],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
    MatCardModule,
    MatGridListModule,
    MatToolbarModule,
    MatButtonModule,
    MatInputModule
  ],
  exports: [
    DataComponent
  ]
})
export class DataModule { }
