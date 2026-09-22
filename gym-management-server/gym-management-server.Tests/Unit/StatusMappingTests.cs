using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Services;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class StatusMappingTests
    {
        [Fact]
        public void A_new_member_defaults_to_Active_not_Suspended()
        {
            Assert.Equal(MemberStatus.Active, new Member().Status);
        }

        [Fact]
        public void A_new_invoice_defaults_to_Pending_not_Paid()
        {
            Assert.Equal(InvoiceStatus.Pending, new Invoice().Status);
        }

        [Fact]
        public void A_new_subscription_defaults_to_Active_not_Expired()
        {
            Assert.Equal(SubscriptionStatus.Active, new MemberDataService().Status);
        }

        [Fact]
        public void Status_survives_the_reflection_mapper()
        {
            // The mapper copies a property only when the source and destination types match
            // exactly. If the entity and the DTO disagree, status is dropped silently.
            var mapper = new GymManagementServiceMapObjects();
            var member = new Member
            {
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0900000001",
                Status = MemberStatus.Suspended,
                Gender = Gender.Female,
            };

            var output = mapper.MapObjects<Member, MemberOutput>(member);

            Assert.Equal(MemberStatus.Suspended, output.Status);
            Assert.Equal(Gender.Female, output.Gender);
        }
    }
}
