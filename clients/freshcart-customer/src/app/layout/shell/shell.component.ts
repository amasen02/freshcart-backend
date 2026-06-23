import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SupportChatWidgetComponent } from '../../features/support/support-chat-widget.component';
import { FooterComponent } from '../footer/footer.component';
import { HeaderComponent } from '../header/header.component';
import { ToastHostComponent } from '../toast-host/toast-host.component';

@Component({
  selector: 'fc-shell',
  imports: [RouterOutlet, HeaderComponent, FooterComponent, ToastHostComponent, SupportChatWidgetComponent],
  templateUrl: './shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {}
