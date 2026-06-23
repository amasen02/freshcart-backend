import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NgbCollapse, NgbDropdown, NgbDropdownItem, NgbDropdownMenu, NgbDropdownToggle } from '@ng-bootstrap/ng-bootstrap';

import { AuthStore } from '../../core/auth/auth.store';
import { BasketStore } from '../../core/basket/basket.store';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

const CatalogRoutePath = '/catalog';

@Component({
  selector: 'fc-header',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    RouterLinkActive,
    NgbCollapse,
    NgbDropdown,
    NgbDropdownToggle,
    NgbDropdownMenu,
    NgbDropdownItem,
    NotificationBellComponent,
  ],
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  protected readonly authStore = inject(AuthStore);
  protected readonly basketStore = inject(BasketStore);
  private readonly router = inject(Router);

  protected readonly navigationCollapsed = signal(true);
  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });

  protected toggleNavigation(): void {
    this.navigationCollapsed.update((isCollapsed) => !isCollapsed);
  }

  protected submitSearch(): void {
    const searchTerm = this.searchControl.value.trim();
    void this.router.navigate(
      [CatalogRoutePath],
      searchTerm.length > 0 ? { queryParams: { term: searchTerm } } : {},
    );
  }

  protected signOut(): void {
    void this.authStore.signOut();
  }
}
