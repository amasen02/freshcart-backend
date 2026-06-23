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

export interface DoughnutChartData {
  readonly labels: readonly string[];
  readonly values: readonly number[];
  readonly title: string;
}

const SegmentColors: readonly string[] = [
  '#1e7a46',
  '#f5a623',
  '#3b82f6',
  '#8b5cf6',
  '#ef4444',
  '#14b8a6',
  '#eab308',
  '#6b7280',
];

@Component({
  selector: 'fc-doughnut-chart',
  template: `
    <div class="position-relative fc-chart-canvas">
      <canvas #canvas role="img" [attr.aria-label]="data().title"></canvas>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoughnutChartComponent {
  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart<'doughnut'> | null = null;

  readonly data = input.required<DoughnutChartData>();

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      ensureChartComponentsRegistered();
      this.chart = this.createChart();
    });

    effect(() => {
      const data = this.data();
      if (this.chart) {
        this.applyData(this.chart, data);
        this.chart.update();
      }
    });

    destroyRef.onDestroy(() => {
      this.chart?.destroy();
      this.chart = null;
    });
  }

  private createChart(): Chart<'doughnut'> {
    const chart = new Chart<'doughnut'>(this.canvas().nativeElement, {
      type: 'doughnut',
      data: { labels: [], datasets: [] },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
      },
    });
    this.applyData(chart, this.data());
    chart.update();
    return chart;
  }

  private applyData(chart: Chart<'doughnut'>, data: DoughnutChartData): void {
    chart.data.labels = [...data.labels];
    chart.data.datasets = [
      {
        data: [...data.values],
        backgroundColor: data.values.map((_, index) => SegmentColors[index % SegmentColors.length] ?? '#6b7280'),
      },
    ];
  }
}
