using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InfoSearch.Web.Models;

namespace InfoSearch.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly VectorSearchService _vectorSearchService;

        public HomeController(ILogger<HomeController> logger, VectorSearchService vectorSearchService)
        {
            _logger = logger;
            _vectorSearchService = vectorSearchService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Search(string searchString = null)
        {
            if (searchString == null)
            {
                return View();
            }

            var result = _vectorSearchService.PerformVectorSearch(searchString);

            return View(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}