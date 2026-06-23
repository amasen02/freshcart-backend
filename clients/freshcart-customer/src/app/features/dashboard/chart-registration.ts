import {
  ArcElement,
  CategoryScale,
  Chart,
  DoughnutController,
  Filler,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from 'chart.js';

let registered = false;

export function ensureChartComponentsRegistered(): void {
  if (registered) {
    return;
  }
  Chart.register(
    LineController,
    DoughnutController,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    ArcElement,
    Filler,
    Tooltip,
    Legend,
  );
  registered = true;
}
