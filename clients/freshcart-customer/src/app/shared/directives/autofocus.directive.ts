import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';

@Directive({ selector: '[fcAutofocus]' })
export class AutofocusDirective {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    afterNextRender(() => this.elementRef.nativeElement.focus());
  }
}
