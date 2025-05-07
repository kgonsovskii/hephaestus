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
                var existingModel = ServerService.GetServerLite(server);
                
                existingModel.CloneModel = model;
                _serverService.CloneServerRequest(Server, existingModel);
                
                _logData = $"Cloning server {model.CloneServerIp} for user {model.CloneUser} at {DateTime.Now}";
                
                TempData["Message"] = "Server cloning initiated successfully!";
                return Ok("ACCEPTED");
            }

            return Ok("FAILED");
        }

        private static string read(string file)
        {
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                string content = reader.ReadToEnd();
                return content;
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
                model.CloneModel.CloneLog =read(model.UserCloneLog);
            }
            catch (Exception e)
            {
                model.CloneModel.CloneLog = "Empty";
            }

            return View("Components/Clone/Viewlog", model.CloneModel);
        }
    }
}