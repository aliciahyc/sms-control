using Microsoft.AspNetCore.Mvc;
using SmsControl.Models;
using SmsControl.Services;

namespace SmsControl.Controllers
{
    [Route("api/sms")]
    [ApiController]
    public class SmsController : ControllerBase
    {
        private readonly SmsService _smsService;

        public SmsController(SmsService smsService)
        {
            _smsService = smsService;
        }

        [HttpPost("allow-send")]
        public ActionResult<SmsResponse> CanSendMessage([FromBody] SmsRequest request)
        {
            if (_smsService.CanSendMessage(request.PhoneNumber, out string message))
            {
                return Ok(new SmsResponse { Success = true, Message = message });
            }

            return BadRequest(new SmsResponse { Success = false, Message = message });
        }

        [HttpPost("reset")]
        public ActionResult<SmsResponse> ResetSmsLimit()
        {
            if (_smsService.ResetLimit(out string message))
            {
                return Ok(new SmsResponse { Success = true, Message = message });
            }

            return BadRequest(new SmsResponse { Success = false, Message = message });
        }

        [HttpGet("get-rate")]
        public ActionResult<SmsResponse> GetThroughput([FromBody] SmsRequest request)
        {
            if (_smsService.MsgProcessedRate(request.PhoneNumber, request.FromDate, request.ToDate, out string message, out double count))
            {
                return Ok(new SmsResponse { Success = true, Message = message, Count = count });
            }

            return BadRequest(new SmsResponse { Success = false, Message = message, Count = count });
        }
    }
}
