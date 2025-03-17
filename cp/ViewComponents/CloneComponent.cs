using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class CloneViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(CloneModel cloneModel)
    {
        return View(cloneModel);
    }
}