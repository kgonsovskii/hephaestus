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
            var existingModel = ServerService.GetServerLite(Server);
            return View("Components/Pack/Default", existingModel.Pack);
        }
        
        [HttpPost("Save")]
        public IActionResult Pack(PackModel model)
        {
            var server = Server;
            var existingModel = ServerService.GetServerLite(server);

            existingModel.Pack.Items = model.Items;
            existingModel.Pack.Refresh();
            _serverService.PackServerRequest(Server, existingModel);

            _logData = $"Packing server at {DateTime.Now}";

            TempData["Message"] = "Server packing initiated successfully!";
            return Index();
        }
        
        [HttpGet("envelope")]
        public async Task<IActionResult> Envelope([FromQuery]string type, [FromQuery] string url)
        {
            url = UrlHelper.NormalizeUri(url);
            var server = Server;
            var model = ServerService.GetServerLite(server);
            var pack = model.Pack.Items.FirstOrDefault(a => a.OriginalUrl == url);
            if (pack == null || (!System.IO.File.Exists(pack.PackFileExe) || !System.IO.File.Exists(pack.PackFileVbs)))
            {
                if (pack == null)
                {
                    pack =new PackItem(model.Pack)
                    {
                        OriginalUrl = url, Enabled = true
                    };
                    model.Pack.Items.Add(pack);
                }
                
                var x = new ServerService();
                x.UpdatePacks(model);
                ServerService.SaveServerLite(server, model);
                x.PackServer(server, pack.Id, null);
                model = ServerService.GetServerLite(server);
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
            var model = ServerService.GetServerLite(server);
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