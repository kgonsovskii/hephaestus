using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers
{
    [Route("[controller]")]
    public class PackController : BaseController
    {
        private static string _logData = "No logs available."; 

        public PackController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(serverService, configuration, memoryCache)
        {
        }

        public IActionResult Index()
        {
            var existingModel = _serverService.GetServer(Server, false, ServerService.Get.RaiseError).ServerModel;
            return View("Components/Pack/Default", existingModel.Pack);
        }


        [HttpPost("Save")]
        public IActionResult Pack(PackModel model)
        {
            var server = Server;
            var existingModel = _serverService.GetServer(server, true, ServerService.Get.RaiseError).ServerModel;

            existingModel.Pack.Items = model.Items;
            _serverService.PackServerRequest(Server, existingModel);

            _logData = $"Packing server at {DateTime.Now}";

            TempData["Message"] = "Server packing initiated successfully!";
            return Index();
        }

        // GET: View Log (Displays log details)
        [HttpGet("ViewLog")]
        public IActionResult ViewLog()
        {
            var server = Server;
            var model = _serverService.GetServer(server, true, ServerService.Get.RaiseError).ServerModel;
            try
            {
                model.Pack.PackLog = System.IO.File.ReadAllText(model.UserPackLog);
            }
            catch (Exception e)
            {
                model.Pack.PackLog = "Empty";
            }

            return View("Components/Pack/Viewlog", model.Pack);
        }
    }
}