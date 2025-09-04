import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  template: `
    <div class="gym-container">
      <div class="page-header">
        <h1>Dashboard - FitGym Manager</h1>
        <p>Tổng quan hoạt động phòng gym của bạn</p>
      </div>
      
      <!-- Stats Cards -->
      <div class="stats-grid">
        <div class="stat-card" *ngFor="let stat of stats">
          <div class="stat-content">
            <div class="stat-value">{{stat.value}}</div>
            <div class="stat-label">{{stat.label}}</div>
            <div class="stat-change" [class]="stat.changeType">{{stat.change}}</div>
          </div>
          <div class="stat-icon">
            <i class="dx-icon dx-icon-{{stat.icon}}"></i>
          </div>
        </div>
      </div>
      
      <!-- Charts -->
      <div class="charts-grid">
        <div class="chart-container">
          <h3>Doanh thu theo tháng</h3>
          <dx-chart [dataSource]="revenueData">
            <dxi-series valueField="revenue" argumentField="month" type="bar"></dxi-series>
            <dxo-legend [visible]="false"></dxo-legend>
            <dxo-tooltip [enabled]="true"></dxo-tooltip>
          </dx-chart>
        </div>
        
        <div class="chart-container">
          <h3>Phân bổ gói tập</h3>
          <dx-pie-chart [dataSource]="membershipData" palette="Bright">
            <dxi-series argumentField="type" valueField="count">
              <dxo-label [visible]="true" format="percent"></dxo-label>
            </dxi-series>
            <dxo-legend 
              [visible]="true" 
              orientation="horizontal" 
              itemTextPosition="right"
              horizontalAlignment="center"
              verticalAlignment="bottom">
            </dxo-legend>
          </dx-pie-chart>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }
    
    .stat-card {
      background: white;
      padding: 24px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    
    .stat-value {
      font-size: 28px;
      font-weight: bold;
      color: #333;
    }
    
    .stat-label {
      font-size: 14px;
      color: #666;
      margin-top: 4px;
    }
    
    .stat-change {
      font-size: 12px;
      margin-top: 8px;
    }
    
    .stat-change.positive { color: #4caf50; }
    .stat-change.negative { color: #f44336; }
    
    .stat-icon {
      width: 60px;
      height: 60px;
      background: linear-gradient(135deg, #3f51b5, #5c6bc0);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    
    .stat-icon i {
      font-size: 24px;
      color: white;
    }
    
    .charts-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
    }
    
    .chart-container {
      background: white;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    
    .chart-container h3 {
      margin-top: 0;
      margin-bottom: 20px;
      color: #333;
    }
    
    @media (max-width: 768px) {
      .charts-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  stats = [
    {
      value: '245',
      label: 'Tổng khách hàng',
      change: '+12% so với tháng trước',
      changeType: 'positive',
      icon: 'group'
    },
    {
      value: '189',
      label: 'Khách hàng hoạt động',
      change: '+8% so với tháng trước',
      changeType: 'positive',
      icon: 'chart'
    },
    {
      value: '₫125.5M',
      label: 'Doanh thu tháng này',
      change: '+15% so với tháng trước',
      changeType: 'positive',
      icon: 'money'
    },
    {
      value: '23',
      label: 'Gói tập sắp hết hạn',
      change: '-5% so với tháng trước',
      changeType: 'negative',
      icon: 'clock'
    }
  ];
  
  revenueData = [
    { month: 'Jan', revenue: 45000000 },
    { month: 'Feb', revenue: 52000000 },
    { month: 'Mar', revenue: 48000000 },
    { month: 'Apr', revenue: 61000000 },
    { month: 'May', revenue: 55000000 },
    { month: 'Jun', revenue: 67000000 }
  ];
  
  membershipData = [
    { type: 'Basic', count: 45 },
    { type: 'Premium', count: 78 },
    { type: 'VIP', count: 23 }
  ];

  ngOnInit() {
    // Initialize dashboard data
  }
}
