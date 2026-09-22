using System.ComponentModel;

namespace gym_management_server.Entities.Enums
{
    // Numeric values are load-bearing: they are already stored in the database
    // and hard-coded in the Angular grid components. Never renumber them.

    public enum MemberStatus : byte
    {
        [Description("Hoạt động")] Active = 0,
        [Description("Tạm ngưng")] Suspended = 1,
        [Description("Hết hạn")] Expired = 2,
    }

    public enum InvoiceStatus : byte
    {
        [Description("Chờ thanh toán")] Pending = 0,
        [Description("Đã thanh toán")] Paid = 1,
        [Description("Đã hủy")] Cancelled = 2,
    }

    public enum SubscriptionStatus : byte
    {
        [Description("Hoạt động")] Active = 0,
        [Description("Hết hạn")] Expired = 1,
        [Description("Đã hủy")] Cancelled = 2,
    }

    public enum PaymentMethod : byte
    {
        [Description("Tiền mặt")] Cash = 0,
        [Description("Chuyển khoản")] BankTransfer = 1,
        [Description("Thẻ")] Card = 2,
    }

    public enum TrainerStatus : byte
    {
        [Description("Đang làm việc")] Working = 0,
        [Description("Ngừng làm việc")] Inactive = 1,
    }

    public enum Gender : byte
    {
        [Description("Nam")] Male = 0,
        [Description("Nữ")] Female = 1,
        [Description("Khác")] Other = 2,
    }

    public enum CheckInMethod : byte
    {
        [Description("Không rõ")] Unknown = 0,
        [Description("Thẻ")] Card = 1,
        [Description("Vân tay")] Fingerprint = 2,
    }
}
