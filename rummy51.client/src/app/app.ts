import { HttpClient } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  standalone: false,
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  // Signals, not plain fields: the app runs zoneless, so a bare assignment
  // from an async callback would never be picked up by change detection.
  public readonly forecasts = signal<WeatherForecast[] | null>(null);
  public readonly loading = signal(false);
  public readonly lastUpdated = signal<Date | null>(null);
  public readonly error = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getForecasts();
  }

  getForecasts() {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe({
      next: (result) => {
        this.forecasts.set(result);
        this.lastUpdated.set(new Date());
        this.loading.set(false);
      },
      error: (error) => {
        console.error(error);
        this.error.set('Could not reach the server. Is it running on https://localhost:7115?');
        this.loading.set(false);
      },
    });
  }

  protected readonly title = signal('rummy51.client');
}
