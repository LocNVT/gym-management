using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace GymManagement.Customers
{
    public class CustomerManager : DomainService
    {
        public readonly ICustomerRepository _customerRepository;

        public CustomerManager(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer> CreateAsync(string code, string fullName, string middleName, string lastName, string firstName,
            DateTime dob, int age, string mobile, string address, string gender)
        {
            var existing = await _customerRepository.AnyAsync(x => x.Code == code);
            if (existing == true)
            {
                throw new Exception("Customer have already existed.");
            }

            return new Customer(GuidGenerator.Create(), code, middleName, lastName, firstName, dob, age, mobile, address, gender);
        }

        public async Task<Customer> UpddateAsync(Customer customer, string code, string fullName, string middleName, string lastName, string firstName,
            DateTime dob, int age, string mobile, string address, string gender)
        {
            var existing = await _customerRepository.AnyAsync(x => x.Id != customer.Id && x.Code == code);
            if (existing == true)
            {
                throw new Exception("Customer have already existed.");
            }

            return new Customer(GuidGenerator.Create(), code, middleName, lastName, firstName, dob, age, mobile, address, gender);
        }
    }
}
