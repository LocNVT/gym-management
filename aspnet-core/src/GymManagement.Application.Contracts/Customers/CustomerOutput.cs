using System;
using Volo.Abp.Application.Dtos;

namespace GymManagement.Customers
{
    class CustomerOutput : EntityDto<Guid>
    {
        public string Code { get; set; }
        public string FullName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public DateTime Dob { get; set; }
        public int Age { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
    }
}
