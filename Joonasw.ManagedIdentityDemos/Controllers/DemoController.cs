using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Joonasw.ManagedIdentityDemos.Models;
using Joonasw.ManagedIdentityDemos.Contracts;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Joonasw.ManagedIdentityDemos.Controllers
{
    public class DemoController : Controller
    {
        private const string MessageTempDataKey = "Message";
        private readonly IDemoService _demoService;
        private readonly DemoSettings _settings;

        public DemoController(
            IDemoService demoService,
            IOptionsSnapshot<DemoSettings> demoSettings,
            IConfiguration configuration)
        {
            _demoService = demoService;
            _settings = demoSettings.Value;
        }

        [HttpGet, HttpHead]
        public IActionResult Index() => View();

        [HttpGet, HttpHead]
        public async Task<IActionResult> KeyVaultConfig()
        {
            var model = await _demoService.AccessKeyVault();
            return View(model);
        }

        [HttpGet, HttpHead]
        public async Task<IActionResult> Storage()
        {
            StorageViewModel model = await _demoService.AccessStorage();
            return View(model);
        }

        [HttpGet, HttpHead]
        public IActionResult ServiceBus() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceBusSend()
        {
            await _demoService.SendServiceBusQueueMessage();

            if (!TempData.ContainsKey(MessageTempDataKey))
            {
                TempData.Add(MessageTempDataKey, "Message sent");
            }

            return RedirectToAction(nameof(ServiceBus));
        }

        [HttpGet, HttpHead]
        public IActionResult EventHubs() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EventHubsSend()
        {
            await _demoService.SendEventHubsMessage();

            if (!TempData.ContainsKey(MessageTempDataKey))
            {
                TempData.Add(MessageTempDataKey, "Message sent");
            }

            return RedirectToAction(nameof(EventHubs));
        }

        [HttpGet, HttpHead]
        public async Task<IActionResult> SqlDatabase()
        {
            SqlDatabaseViewModel model = await _demoService.AccessSqlDatabase();
            return View(model);
        }

        [HttpGet, HttpHead]
        public async Task<IActionResult> CosmosDb()
        {
            CosmosDbViewModel model = await _demoService.AccessCosmosDb();
            return View(model);
        }

        [HttpGet, HttpHead]
        public async Task<IActionResult> CustomService()
        {
            CustomServiceViewModel model = await _demoService.AccessCustomApi();
            return View(model);
        }

        [HttpGet, HttpHead]
        public IActionResult ServiceBusListen() => View();

        [HttpGet, HttpHead]
        public IActionResult EventHubsListen() => View();

        [HttpGet, HttpHead]
        public async Task<IActionResult> DataLake()
        {
            DataLakeViewModel model = await _demoService.AccessDataLake();
            return View(model);
        }

        [HttpGet, HttpHead]
        public IActionResult CognitiveServices() => View(new CognitiveServicesInputModel());

        [HttpPost]
        public async Task<IActionResult> CognitiveServices([FromForm] CognitiveServicesInputModel model)
        {
            CognitiveServicesResultsViewModel resultsModel =
                await _demoService.AccessCognitiveServices(model.Input);
            return View("CognitiveServicesResults", resultsModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
