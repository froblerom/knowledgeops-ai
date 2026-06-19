import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/services/auth.service';
import { RoleVisibilityService } from './core/services/role-visibility.service';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly authService = inject(AuthService);
  protected readonly visibility = inject(RoleVisibilityService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.authService.logout().subscribe({
      complete: () => void this.router.navigate(['/login']),
      error: () => void this.router.navigate(['/login']),
    });
  }
}
