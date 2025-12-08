using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;

namespace ARISESLCOM.Controllers
{
    public class HomeController(IOrderDomainModel orderDomainModel,
                                IDBContext dbContext,
                                IRedisCacheService redis) : BasicController(redis)
    {
        private readonly IOrderDomainModel _orderDomainModel = orderDomainModel;
        private readonly IDBContext _dbContext = dbContext;

        [Authorize]
        public async Task<IActionResult> IndexAsync()
        {
            await _dbContext.OpenAsync();
            HomeViewModel homeModel;
            UserModel user = new();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                var orderStatusReport = await _orderDomainModel.GetOrderStatusReportDBAsync();
                var orderStatusTodayReport = await _orderDomainModel.GetOrderStatusTodayReportDBAsync();
                homeModel = new HomeViewModel(orderStatusReport, orderStatusTodayReport, user);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(homeModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
