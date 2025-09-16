import { Component, OnInit } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';


@Component({
  selector: 'app-offers-comparison-chart',
  templateUrl: './offers-comparison-chart.component.html',
  styleUrls: ['./offers-comparison-chart.component.css']
})
export class OffersComparisonChartComponent implements OnInit {
  barChartOptions: ChartOptions = {
    responsive: true,
  };
  barChartLabels =  ['Apple', 'Banana', 'Kiwifruit', 'Blueberry', 'Orange', 'Grapes'];
  barChartType: ChartType = 'bar';
  barChartLegend = true;
  barChartPlugins = [];
  barChartData: ChartDataset[] = [
    { data: [45, 37, 60, 70, 46, 33], label: 'Best Fruits' }
  ];
  constructor() { }

  ngOnInit(): void {
  }

}
