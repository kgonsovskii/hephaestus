using cp.Controllers;
using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class PackViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ServerModel serverModel)
    {
        return View(serverModel.Pack);
    }
}