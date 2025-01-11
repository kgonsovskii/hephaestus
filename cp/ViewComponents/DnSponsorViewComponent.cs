using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class DnSponsorViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<DnSponsorModel> dnSponsorModels)
    {
        return View(dnSponsorModels);
    }
}