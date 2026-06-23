import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NgbConfig, NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { ConfirmDialogService } from './confirm-dialog.service';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

describe('ConfirmDialogService', () => {
  let service: ConfirmDialogService;
  let modalService: NgbModal;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    // Modal open/close transitions would outlive TestBed teardown and detach DOM mid-animation.
    TestBed.inject(NgbConfig).animation = false;
    service = TestBed.inject(ConfirmDialogService);
    modalService = TestBed.inject(NgbModal);
  });

  afterEach(async () => {
    modalService.dismissAll();
    await settle();
  });

  it('resolvesTrueWhenTheUserConfirms', async () => {
    const confirmation = service.confirm({ title: 'Remove item', message: 'Remove this item from the basket?' });
    await settle();

    const confirmButton = document.querySelector<HTMLButtonElement>('.modal-footer .btn-primary');
    expect(confirmButton).not.toBeNull();
    confirmButton?.click();

    await expectAsync(confirmation).toBeResolvedTo(true);
  });

  it('resolvesFalseWhenTheUserCancels', async () => {
    const confirmation = service.confirm({ title: 'Remove item', message: 'Remove this item from the basket?' });
    await settle();

    document.querySelector<HTMLButtonElement>('.modal-footer .btn-outline-secondary')?.click();

    await expectAsync(confirmation).toBeResolvedTo(false);
  });

  it('usesTheDangerStyleForDestructiveConfirmations', async () => {
    const confirmation = service.confirm({
      title: 'Cancel order',
      message: 'This cannot be undone.',
      confirmLabel: 'Cancel order',
      destructive: true,
    });
    await settle();

    const dangerButton = document.querySelector<HTMLButtonElement>('.modal-footer .btn-danger');
    expect(dangerButton?.textContent).toContain('Cancel order');
    dangerButton?.click();

    await expectAsync(confirmation).toBeResolvedTo(true);
  });

  it('rendersTheProvidedTitleAndMessage', async () => {
    const confirmation = service.confirm({ title: 'Sign out', message: 'End your current session?' });
    await settle();

    expect(document.querySelector('.modal-title')?.textContent).toContain('Sign out');
    expect(document.querySelector('.modal-body')?.textContent).toContain('End your current session?');

    modalService.dismissAll();
    await expectAsync(confirmation).toBeResolvedTo(false);
  });
});
