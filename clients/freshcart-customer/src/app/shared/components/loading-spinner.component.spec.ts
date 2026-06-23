import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { LoadingSpinnerComponent } from './loading-spinner.component';

describe('LoadingSpinnerComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('announcesTheDefaultLabelToScreenReaders', () => {
    const fixture = TestBed.createComponent(LoadingSpinnerComponent);
    fixture.detectChanges();

    const hiddenLabel = (fixture.nativeElement as HTMLElement).querySelector('.visually-hidden');
    expect(hiddenLabel?.textContent).toContain('Loading');
  });

  it('announcesACustomLabelWhenOneIsProvided', () => {
    const fixture = TestBed.createComponent(LoadingSpinnerComponent);
    fixture.componentRef.setInput('label', 'Loading your basket');
    fixture.detectChanges();

    const hiddenLabel = (fixture.nativeElement as HTMLElement).querySelector('.visually-hidden');
    expect(hiddenLabel?.textContent).toContain('Loading your basket');
  });
});
