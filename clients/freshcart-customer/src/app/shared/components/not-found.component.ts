import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { EmptyStateComponent } from './empty-state.component';

@Component({
  selector: 'fc-not-found',
  imports: [EmptyStateComponent, RouterLink],
  template: `
    <section class="container py-5">
      <fc-empty-state
        icon="compass"
        heading="Page not found"
        message="The page you are looking for does not exist or has moved.">
        <a routerLink="/" class="btn btn-primary">Back to home</a>
      </fc-empty-state>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
