using System.Net;
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
            var existingModel = _serverService.GetServer(server, false, ServerService.Get.RaiseError).ServerModel;

            existingModel.Pack.Items = model.Items;
            _serverService.PackServerRequest(Server, existingModel);

            _logData = $"Packing server at {DateTime.Now}";

            TempData["Message"] = "Server packing initiated successfully!";
            return Index();
        }
        
        [HttpGet("envelope")]
        public async Task<IActionResult> Envelope([FromQuery]string type, [FromQuery] string url)
        {
            var server = Server;
            url = UrlHelper.NormalizeUrl(url);
            var model = _serverService.GetServer(server, false, ServerService.Get.RaiseError).ServerModel;
            var pack = model.Pack.Items.FirstOrDefault(a => a.OriginalUrl == url);
            if (pack == null || (!System.IO.File.Exists(pack.PackFileExe) || !System.IO.File.Exists(pack.PackFileVbs)))
            {
                if (pack == null)
                {
                    pack =new PackItem()
                    {
                        OriginalUrl = url, Enabled = true, Index = Guid.NewGuid().ToString()
                    };
                    model.Pack.Items.Add(pack);
                }
                if (!Directory.Exists(model.Pack.PackRootFolder))
                    Directory.CreateDirectory(model.Pack.PackRootFolder);
                var x = new ServerService();
                x.UpdatePacks(model);
                ServerService.SaveServerLite(server, model);
                x.PackServer(server, pack.Index);
                model = _serverService.GetServer(server, false, ServerService.Get.RaiseError).ServerModel;
                pack = model.Pack.Items.FirstOrDefault(a => a.OriginalUrl == url);
            }
            if (type == "vbs")
                return await GetFileX(pack.PackFileVbs, pack.Name, type);
            else
            {
                return await GetFileX(pack.PackFileExe, pack.Name, type);
            }
        }

        // GET: View Log (Displays log details)
        [HttpGet("ViewLog")]
        public IActionResult ViewLog()
        {
            var server = Server;
            var model = _serverService.GetServer(server, false, ServerService.Get.RaiseError).ServerModel;
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