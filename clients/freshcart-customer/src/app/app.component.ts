import { ChangeDetectionStrategy, Component } from '@angular/core';

import { ShellComponent } from './layout/shell/shell.component';

@Component({
  selector: 'fc-root',
  imports: [ShellComponent],
  template: '<fc-shell />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
