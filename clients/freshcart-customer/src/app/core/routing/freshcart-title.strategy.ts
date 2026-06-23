import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

const BrandSuffix = ' · FreshCart';
const BrandName = 'FreshCart';

@Injectable({ providedIn: 'root' })
export class FreshcartTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(routerState: RouterStateSnapshot): void {
    const resolvedTitle = this.buildTitle(routerState);
    this.title.setTitle(resolvedTitle ? `${resolvedTitle}${BrandSuffix}` : BrandName);
  }
}
