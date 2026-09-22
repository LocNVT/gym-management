using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    /// <summary>
    /// Single source of truth for status labels. The Angular grids read their dropdown
    /// options from here instead of each component hard-coding its own copy.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnumsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll() => Ok(new Dictionary<string, IReadOnlyList<EnumValueInfo>>
        {
            ["memberStatus"] = EnumLabel.Describe<MemberStatus>(),
            ["invoiceStatus"] = EnumLabel.Describe<InvoiceStatus>(),
            ["subscriptionStatus"] = EnumLabel.Describe<SubscriptionStatus>(),
            ["paymentMethod"] = EnumLabel.Describe<PaymentMethod>(),
            ["trainerStatus"] = EnumLabel.Describe<TrainerStatus>(),
            ["gender"] = EnumLabel.Describe<Gender>(),
            ["checkInMethod"] = EnumLabel.Describe<CheckInMethod>(),
        });
    }
}
