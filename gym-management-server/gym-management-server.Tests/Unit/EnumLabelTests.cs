using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class EnumLabelTests
    {
        [Fact]
        public void ToLabel_returns_the_Description_attribute()
        {
            Assert.Equal("Đã thanh toán", InvoiceStatus.Paid.ToLabel());
            Assert.Equal("Chờ thanh toán", InvoiceStatus.Pending.ToLabel());
            Assert.Equal("Hoạt động", MemberStatus.Active.ToLabel());
        }

        [Fact]
        public void ToLabel_on_null_returns_empty_string()
        {
            Gender? missing = null;
            Assert.Equal("", missing.ToLabel());
        }

        [Fact]
        public void Describe_lists_every_value_with_its_numeric_value()
        {
            var values = EnumLabel.Describe<InvoiceStatus>();

            Assert.Equal(3, values.Count);
            Assert.Equal((byte)0, values[0].Value);
            Assert.Equal("Pending", values[0].Name);
            Assert.Equal("Chờ thanh toán", values[0].Label);
        }

        [Theory]
        [InlineData("Đã thanh toán", InvoiceStatus.Paid)]
        [InlineData("  đã thanh toán  ", InvoiceStatus.Paid)]
        [InlineData("ĐÃ HỦY", InvoiceStatus.Cancelled)]
        public void TryParseLabel_accepts_any_casing_and_surrounding_space(string input, InvoiceStatus expected)
        {
            Assert.True(EnumLabel.TryParseLabel<InvoiceStatus>(input, out var result));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseLabel_rejects_an_unknown_label()
        {
            Assert.False(EnumLabel.TryParseLabel<InvoiceStatus>("Chưa rõ", out _));
        }

        [Fact]
        public void Enum_numeric_values_match_the_labels_the_Angular_grids_already_use()
        {
            // These numbers are already in the database and in the Angular components.
            // Changing them would silently reinterpret existing rows.
            Assert.Equal(0, (byte)MemberStatus.Active);
            Assert.Equal(1, (byte)MemberStatus.Suspended);
            Assert.Equal(0, (byte)InvoiceStatus.Pending);
            Assert.Equal(1, (byte)InvoiceStatus.Paid);
            Assert.Equal(0, (byte)SubscriptionStatus.Active);
            Assert.Equal(1, (byte)SubscriptionStatus.Expired);
            Assert.Equal(2, (byte)CheckInMethod.Fingerprint);
        }
    }
}
