using System;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Enums;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.Members;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class MemberServiceTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static MemberService BuildService(GymManagementContext db) =>
            new(new MemberRepository(db), new GymManagementServiceMapObjects());

        private static MemberInput SampleInput() => new MemberInput
        {
            FullName = "Nguyễn Thị B",
            PhoneNumber = "0900000010",
            Gender = Gender.Female,
            Notes = "Dị ứng phấn hoa",
            Status = MemberStatus.Active,
        };

        [Fact]
        public async Task Create_persists_gender_and_notes()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var created = await service.CreateMemberAsync(SampleInput());

            var stored = await db.Members.SingleAsync(m => m.Id == created.Id);
            Assert.Equal(Gender.Female, stored.Gender);
            Assert.Equal("Dị ứng phấn hoa", stored.Notes);
        }

        [Fact]
        public async Task Update_persists_gender_and_notes()
        {
            using var db = NewContext();
            var service = BuildService(db);
            var created = await service.CreateMemberAsync(new MemberInput
            {
                FullName = "Trần Văn C",
                PhoneNumber = "0900000011",
                Gender = Gender.Male,
                Notes = "Ban đầu",
                Status = MemberStatus.Active,
            });

            await service.UpdateMemberAsync(created.Id, new MemberInput
            {
                FullName = "Trần Văn C",
                PhoneNumber = "0900000011",
                Gender = Gender.Other,
                Notes = "Đã cập nhật",
                Status = MemberStatus.Active,
            });

            var stored = await db.Members.SingleAsync(m => m.Id == created.Id);
            Assert.Equal(Gender.Other, stored.Gender);
            Assert.Equal("Đã cập nhật", stored.Notes);
        }
    }
}
