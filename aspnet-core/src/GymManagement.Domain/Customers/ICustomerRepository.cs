using System;
using Volo.Abp.Domain.Repositories;

namespace GymManagement.Customers
{
    public interface ICustomerRepository : IRepository<Customer, Guid>
    {
    }
}
