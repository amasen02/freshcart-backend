import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrderStatus } from '../orders/orders.models';
import { OrderStatusTimelineComponent } from './order-status-timeline.component';

describe('OrderStatusTimelineComponent', () => {
  let fixture: ComponentFixture<OrderStatusTimelineComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    fixture = TestBed.createComponent(OrderStatusTimelineComponent);
  });

  function renderWith(status: OrderStatus): void {
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
  }

  it('marksSubmittedAsTheCurrentStepWithNothingCompletedYet', () => {
    renderWith('Submitted');

    const completed = fixture.nativeElement.querySelectorAll('.bi-check-lg').length;
    const inProgress = fixture.nativeElement.querySelectorAll('.spinner-border').length;
    expect(completed).toBe(0);
    expect(inProgress).toBe(1);
  });

  it('marksEveryStepCompleteWhenConfirmed', () => {
    renderWith('Confirmed');

    const completed = fixture.nativeElement.querySelectorAll('.bi-check-lg').length;
    const inProgress = fixture.nativeElement.querySelectorAll('.spinner-border').length;
    expect(completed).toBe(4);
    expect(inProgress).toBe(0);
  });

  it('marksPaidWithTwoCompletedStepsAndPaidItselfInProgress', () => {
    renderWith('Paid');

    expect(fixture.nativeElement.querySelectorAll('.bi-check-lg').length).toBe(2);
    expect(fixture.nativeElement.querySelectorAll('.spinner-border').length).toBe(1);
  });

  it('rendersACancelledNoticeInsteadOfTheTimeline', () => {
    renderWith('Cancelled');

    const alert: HTMLElement | null = fixture.nativeElement.querySelector('.alert-danger');
    expect(alert).not.toBeNull();
    expect(fixture.nativeElement.querySelector('ol')).toBeNull();
  });

  it('treatsRefundedAsACancelledOutcome', () => {
    renderWith('Refunded');

    expect(fixture.nativeElement.querySelector('.alert-danger')).not.toBeNull();
  });
});
