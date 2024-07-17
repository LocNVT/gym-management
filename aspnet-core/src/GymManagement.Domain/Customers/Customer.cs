using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace GymManagement.Customers
{
    public class Customer : AuditedAggregateRoot<Guid>
    {
        public Customer(Guid id, string code, string middleName, string lastName, string firstName,
            DateTime dob, int age, string mobile, string address, string gender)
        {
            Id = id;
            Code = code;
            MiddleName = middleName;
            LastName = lastName;
            FirstName = firstName;
            Dob = dob;
            Age = age;
            Mobile = mobile;
            Address = address;
            Gender = gender;
        }

        public virtual string Code { get; internal set; }
        public virtual string FullName { get; internal set; }
        public virtual string MiddleName { get; internal set; }
        public virtual string LastName { get; internal set; }
        public virtual string FirstName { get; internal set; }
        public virtual DateTime Dob { get; internal set; }
        public virtual int Age { get; internal set; }
        public virtual string Mobile { get; internal set; }
        public virtual string Address { get; internal set; }
        public virtual string Gender { get; internal set; }

        public void SetFullName(string lastName, string middleName, string firstName)
        {
            if (!string.IsNullOrEmpty(lastName))
            {
                lastName = $"{lastName} ";
            }
            if (!string.IsNullOrEmpty(middleName))
            {
                middleName = $"{middleName} ";
            }
            var fullName = $"{lastName}{middleName}{firstName}";
            FullName = fullName.Trim();
        }
    }
}
