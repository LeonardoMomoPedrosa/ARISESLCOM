using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Controllers
{
    [Authorize]
    public class ReportController(IDBContext dBContext,
                                    IReportDomainModel reportDomainModel,
                                    IRedisCacheService redis) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IReportDomainModel _reportDomainModel = reportDomainModel;

        public IActionResult DaySelect()
        {
            return View();
        }

        public IActionResult MonthSelect()
        {
            return View();
        }

        public IActionResult GroupSelect()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DayReport(DateModel inModel)
        {
            List<DayReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            ViewBag.Data1 = inModel.Data1;
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetDayReportModel(inModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(outModel);
        }

        [HttpPost]
        public async Task<IActionResult> MonthReport(DateModel inModel)
        {
            List<MonthReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            ViewBag.Month = inModel.Month;
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetMonthReportModel(inModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(outModel);
        }


        private async Task<List<GroupItemReportModel>> GetGroupItemReportModel(DateModel inModel)
        {
            List<GroupItemReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            ViewBag.Month = inModel.Month;
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetGroupItemReportModel(inModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return outModel;
        }

        [HttpPost]
        public async Task<IActionResult> GroupItemSelect(DateModel inModel)
        {
            return View(await GetGroupItemReportModel(inModel));
        }

        [HttpGet]
        [Route("/Report/GroupItemNavSelect/{month}")]
        public async Task<IActionResult> GroupItemNavSelect(string month)
        {
            DateModel model = new()
            {
                Month = DateTime.ParseExact(month, "MMyyyy", null)
            };
            return View("GroupItemSelect", await GetGroupItemReportModel(model));
        }

        [HttpGet]
        [Route("/Report/GroupDetails/{id}")]
        public async Task<IActionResult> GroupDetails(string id)
        {
            List<GroupItemReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                var idSubtipo = int.Parse(id.Split('-')[0]);
                var monthDt = id.Split('-')[1];
                ViewBag.Subtipo = idSubtipo;
                DateModel dateM = new()
                {
                    Month = DateTime.ParseExact(monthDt, "MMyyyy", null)
                };
                ViewBag.Month = dateM.Month;

                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetGroupDetailReportModel(idSubtipo, dateM);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(outModel);
        }

        [HttpGet]
        public async Task<IActionResult> YearReport()
        {
            List<YearReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetYearReportModel();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(outModel);
        }

        [HttpGet]
        public async Task<IActionResult> AviseMeReport()
        {
            List<AviseMeReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                outModel = await _reportDomainModel.GetAviseMeReportModel();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(outModel);
        }

        public IActionResult CupomReportSelect()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CupomReport(CupomReportRequestModel inModel)
        {
            List<DayReportModel> outModel;
            _dbContext.GetSqlConnection().Open();
            ViewBag.Month = inModel.Month;
            ViewBag.Cupom = inModel.Cupom;
            try
            {
                _reportDomainModel.SetContext(_dbContext);
                var dateModel = new DateModel { Month = inModel.Month };
                outModel = await _reportDomainModel.GetCupomReportModel(inModel.Cupom, dateModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(outModel);
        }

    }
}
