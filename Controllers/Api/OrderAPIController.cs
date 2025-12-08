using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;

namespace ARISESLCOM.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IDBContext dbContext,
                                    IOrderDomainModel orderDomain) : ControllerBase
    {
        private readonly IDBContext _dbContext = dbContext;
        private readonly IOrderDomainModel _orderDomain = orderDomain;

        [HttpPost("orderstatus")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangeOrderStatusAsync([FromBody] ChangeOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!request.NewStatus.Equals(SLCOMLIB.Helpers.LibConsts.ORDER_STATUS_CANC))
            {
                return Ok();
            }

            _dbContext.GetSqlConnection().Open();

            ActionResultModel mod = new();

            try
            {
                _orderDomain.SetContext(_dbContext);
                mod = await _orderDomain.ReturnCreditAsync(request.OrderId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            if (mod == null || mod.IsSuccess)
            {
                return Ok();
            }
            else
            {
                return BadRequest(mod.Message);
            }
        }
    }
}
