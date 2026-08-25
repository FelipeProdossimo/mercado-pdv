import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [
    RouterLink,
    RouterOutlet
  ],
  templateUrl: './admin-layout.html'
})
export class AdminLayout {
}