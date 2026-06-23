import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'fc-footer',
  templateUrl: './footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {
  protected readonly currentYear = new Date().getFullYear();
}
