import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { Chart } from 'chart.js';

import { ensureChartComponentsRegistered } from './chart-registration';

export interface LineChartSeries {
  readonly labels: readonly string[];
  readonly values: readonly number[];
  readonly datasetLabel: string;
}

@Component({
  selector: 'fc-line-chart',
  template: `
    <div class="position-relative fc-chart-canvas">
      <canvas #canvas role="img" [attr.aria-label]="series().datasetLabel"></canvas>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LineChartComponent {
  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart<'line'> | null = null;

  readonly series = input.required<LineChartSeries>();

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      ensureChartComponentsRegistered();
      this.chart = this.createChart();
    });

    effect(() => {
      const series = this.series();
      if (this.chart) {
        this.applySeries(this.chart, series);
        this.chart.update();
      }
    });

    destroyRef.onDestroy(() => {
      this.chart?.destroy();
      this.chart = null;
    });
  }

  private createChart(): Chart<'line'> {
    const chart = new Chart<'line'>(this.canvas().nativeElement, {
      type: 'line',
      data: { labels: [], datasets: [] },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true } },
      },
    });
    this.applySeries(chart, this.series());
    chart.update();
    return chart;
  }

  private applySeries(chart: Chart<'line'>, series: LineChartSeries): void {
    chart.data.labels = [...series.labels];
    chart.data.datasets = [
      {
        label: series.datasetLabel,
        data: [...series.values],
        borderColor: '#1e7a46',
        backgroundColor: 'rgba(30, 122, 70, 0.15)',
        fill: true,
        tension: 0.3,
      },
    ];
  }
}
