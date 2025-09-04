import { Component, OnInit, ViewChild } from '@angular/core';
import { DataService } from '../../../../shared/services/data.service';
import { CustomerApiService, CustomerStats } from '../../../../shared/services/customer-api.service';
import { CustomerUpdateComponent } from '../customer-form/customer-update.component';
import { Customer } from '../../models/customer.model';

@Component({
  selector: 'app-customer-list',
  templateUrl: './customer-list.component.html',
})
export class CustomerListComponent implements OnInit {
  @ViewChild('customerUpdate') customerUpdate: CustomerUpdateComponent | undefined;
  customersDataSource: any;
  customerStats: CustomerStats | undefined;

  constructor(
    private dataService: DataService,
    private customerApi: CustomerApiService
  ) {}

  ngOnInit() {
    this.customersDataSource = this.dataService.getCustomersDataSource();
    this.loadCustomerStats();
  }

  loadCustomerStats() {
    this.customerApi.getCustomerStats().subscribe(
      stats => {
        this.customerStats = stats;
        console.log('Customer stats:', stats);
      },
      error => {
        console.error('Error loading stats:', error);
      }
    );
  }

  editCustomer = (e: any) => {
    if (this.customerUpdate) {
      const customerToEdit = e.row.data as Customer;
      this.customerUpdate.openPopup(customerToEdit);
    }
  };

  deleteCustomer = (e: any) => {
    // The remove operation is handled by the CustomStore
    // You can add a confirmation dialog here if you want
    console.log('Delete customer:', e.row.data);
  };

  viewCustomer = (e: any) => {
    if (this.customerUpdate) {
      const customerToView = e.row.data as Customer;
      this.customerUpdate.openPopup(customerToView); // open in read-only mode
    }
  };
}
