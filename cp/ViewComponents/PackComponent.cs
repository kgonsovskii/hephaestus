using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class PackViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PackModel packModel)
    {
        return View(packModel);
    }
}