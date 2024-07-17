using GymManagement.EntityFrameworkCore;
using System;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace GymManagement.Customers
{
    public class EfCoreCustomerRepository : EfCoreRepository<GymManagementDbContext, Customer, Guid>, ICustomerRepository
    {
        public EfCoreCustomerRepository(IDbContextProvider<GymManagementDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
