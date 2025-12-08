using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Services.interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ARISESLCOM.Controllers
{
    [Authorize]
    public class CustomerController(IDBContext dBContext,
                                    ICustomerDomainModel customerDomainModel,
                                    IRedisCacheService redis) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly ICustomerDomainModel _customerDomainModel = customerDomainModel;        


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SearchInput()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(CustomerSearchViewModel inModel)
        {
            List<CustomerModel> outModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _customerDomainModel.SetContext(_dbContext);
                outModel = await _customerDomainModel.GetCustomerListAsync(inModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(outModel);
        }

        [HttpPatch]
        public async Task<IActionResult> ChangeTrust(int customerId, bool trustInd)
        {
            ActionResultModel resModel = new(ActionResultModel.SUCCESS, "Altera��o feita com sucesso");

            _dbContext.GetSqlConnection().Open();
            try
            {
                _customerDomainModel.SetContext(_dbContext);
                await _customerDomainModel.UpdateCustomerDBTrust(customerId, trustInd);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(new { success = resModel.IsSuccess, message = resModel.Message });
        }

        [HttpPatch]
        public async Task<IActionResult> ChangeDiscount(int customerId, decimal discount)
        {
            ActionResultModel resModel = new(ActionResultModel.SUCCESS, "Altera��o feita com sucesso");

            _dbContext.GetSqlConnection().Open();
            try
            {
                _customerDomainModel.SetContext(_dbContext);
                await _customerDomainModel.UpdateCustomerDBDiscount(customerId, discount);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(new { success = resModel.IsSuccess, message = resModel.Message });
        }

        [HttpPatch]
        public async Task<IActionResult> AddCredit(int customerId, decimal credit)
        {
            ActionResultModel resModel = new(ActionResultModel.SUCCESS, "Altera��o feita com sucesso");
            decimal newCredit = 0;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _customerDomainModel.SetContext(_dbContext);
                if (credit != 0)
                {
                    newCredit = await _customerDomainModel.InsertCustomerDBCredit(customerId, credit);
                } else
                {
                    newCredit = await _customerDomainModel.GetCustomerDBCredit(customerId);
                }
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(new { success = resModel.IsSuccess, message = resModel.Message, newCredit });
        }
    }
}
