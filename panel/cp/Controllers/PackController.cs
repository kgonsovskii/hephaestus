using Commons;
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
            var existingModel = _serverService.GetServerLite();
            return View("Components/Pack/Default", existingModel.Pack);
        }
        
        [HttpPost("Save")]
        public IActionResult Pack(PackModel model)
        {
            var server = Server;
            var existingModel = _serverService.GetServerLite();

            existingModel.Pack.Items = model.Items;
            existingModel.Pack.Refresh();
            _serverService.PackServerRequest(existingModel);

            _logData = $"Packing server at {DateTime.Now}";

            TempData["Message"] = "Server packing initiated successfully!";
            return Index();
        }
        
        [HttpGet("envelope")]
        public async Task<IActionResult> Envelope([FromQuery]string type, [FromQuery] string url)
        {
            url = UrlHelper.NormalizeUri(url);
            var server = Server;
            var model = _serverService.GetServerLite();
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
                
                _serverService.UpdatePacks(model);
                _serverService.SaveServerLite(model);
                _serverService.PackServer(pack.Id, null);
                model = _serverService.GetServerLite();
                pack = model.Pack.Items.FirstOrDefault(a => a.OriginalUrl == url);
            }
            if (type == "vbs")
                return await GetFileX(pack.PackFileVbs, pack.Name, type);
            else
            {
                return await GetFileX(pack.PackFileExe, pack.Name, type);
            }
        }

        
        [HttpGet("ViewLog")]
        public IActionResult ViewLog()
        {
            var server = Server;
            var model = _serverService.GetServerLite();
            try
            {
                model.Pack.PackLog = System.IO.File.ReadAllText(_serverService.UserPackLogPath);
            }
            catch (Exception e)
            {
                model.Pack.PackLog = "Empty";
            }

            return View("Components/Pack/Viewlog", model.Pack);
        }
    }
}
