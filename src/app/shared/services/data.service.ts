import { Injectable } from '@angular/core';
import CustomStore from 'devextreme/data/custom_store';
import { CustomerApiService } from './customer-api.service';
import { MembershipApiService } from './membership-api.service';
import { AttendanceApiService } from './attendance-api.service';
import { FinanceApiService } from './finance-api.service';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  constructor(
    private customerApi: CustomerApiService,
    private membershipApi: MembershipApiService,
    private attendanceApi: AttendanceApiService,
    private financeApi: FinanceApiService
  ) {}

  // Create CustomStore for DevExtreme DataGrid - Customers
  getCustomersDataSource() {
    return new CustomStore({
      key: 'id',
      load: (loadOptions: any) => {
        const params = this.buildLoadParams(loadOptions);
        return this.customerApi.getCustomers(params).toPromise()
          .then(response => ({
            data: response?.customers || [],
            totalCount: response?.total || 0
          }));
      },
      insert: (values: any) => {
        return this.customerApi.createCustomer(values).toPromise();
      },
      update: (key: any, values: any) => {
        return this.customerApi.updateCustomer(key, values).toPromise();
      },
      remove: (key: any) => {
        return this.customerApi.deleteCustomer(key).toPromise();
      }
    });
  }

  // Create CustomStore for DevExtreme DataGrid - Transactions
  getTransactionsDataSource() {
    return new CustomStore({
      key: 'id',
      load: (loadOptions: any) => {
        const params = this.buildLoadParams(loadOptions);
        return this.financeApi.getTransactions(params).toPromise()
          .then(response => ({
            data: response || [],
            totalCount: response?.length || 0
          }));
      },
      insert: (values: any) => {
        return this.financeApi.createTransaction(values).toPromise();
      },
      update: (key: any, values: any) => {
        return this.financeApi.updateTransaction(key, values).toPromise();
      }
    });
  }

  // Create CustomStore for DevExtreme DataGrid - Attendance
  getAttendanceDataSource() {
    return new CustomStore({
      key: 'id',
      load: (loadOptions: any) => {
        const params = this.buildLoadParams(loadOptions);
        return this.attendanceApi.getAttendanceHistory(params).toPromise()
          .then(response => ({
            data: response || [],
            totalCount: response?.length || 0
          }));
      }
    });
  }

  private buildLoadParams(loadOptions: any): any {
    const params: any = {};

    // Pagination
    if (loadOptions.skip !== undefined && loadOptions.take !== undefined) {
      params.page = Math.floor(loadOptions.skip / loadOptions.take) + 1;
      params.pageSize = loadOptions.take;
    }

    // Sorting
    if (loadOptions.sort) {
      const sort = loadOptions.sort[0];
      params.sortField = sort.selector;
      params.sortOrder = sort.desc ? 'desc' : 'asc';
    }

    // Filtering
    if (loadOptions.filter) {
      // Convert DevExtreme filter format to API format
      params.filter = JSON.stringify(loadOptions.filter);
    }

    // Searching
    if (loadOptions.searchValue) {
      params.search = loadOptions.searchValue;
    }

    return params;
  }
}
