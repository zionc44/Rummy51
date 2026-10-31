import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register').then((m) => m.Register),
  },
  {
    path: 'lobby',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/lobby/lobby').then((m) => m.Lobby),
  },
  { path: '**', redirectTo: '' },
];
