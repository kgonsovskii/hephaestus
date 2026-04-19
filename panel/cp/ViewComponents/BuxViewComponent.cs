using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class BuxViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<BuxModel> buxModels)
    {
        return View(buxModels);
    }
}