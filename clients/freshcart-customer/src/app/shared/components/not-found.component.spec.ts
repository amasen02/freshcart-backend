import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    });
  });

  it('explainsTheMissingPageAndOffersAWayHome', () => {
    const fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
    const componentElement = fixture.nativeElement as HTMLElement;

    expect(componentElement.querySelector('h2')?.textContent).toContain('Page not found');
    expect(componentElement.querySelector('a[href="/"]')?.textContent).toContain('Back to home');
  });
});
