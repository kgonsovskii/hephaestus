using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers
{
    [Route("[controller]")]
    public class CloneController : BaseController
    {
        private static string _logData = "No logs available."; // Simulated log storage

        // GET: Clone page
        public CloneController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(serverService, configuration, memoryCache)
        {
        }

        public IActionResult Index()
        {
            return View("Components/Clone/Default", new CloneModel());
        }

        // POST: Clone Server (Handles the cloning process)
        [HttpPost]
        public IActionResult CloneServer([FromBody]CloneModel model)
        {
            if (ModelState.IsValid)
            {
                var server = Server;
                var existingModel = _serverService.GetServer(server, true, ServerService.Get.RaiseError).ServerModel;
                
                existingModel.CloneModel = model;
                _serverService.CloneServerRequest(Server, existingModel);
                
                _logData = $"Cloning server {model.CloneServerIp} for user {model.CloneUser} at {DateTime.Now}";
                
                TempData["Message"] = "Server cloning initiated successfully!";
                return Ok("ACCEPTED");
            }

            return Ok("FAILED");
        }

        // GET: View Log (Displays log details)
        [HttpGet("ViewLog")]
        public IActionResult ViewLog()
        {
            var server = Server;
            var model = _serverService.GetServer(server, true, ServerService.Get.RaiseError).ServerModel;
            try
            {
                model.CloneModel.CloneLog = System.IO.File.ReadAllText(model.UserCloneLog);
            }
            catch (Exception e)
            {
                model.CloneModel.CloneLog = "Empty";
            }

            return View("Components/Clone/Viewlog", model.CloneModel);
        }
    }
}