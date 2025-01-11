using Microsoft.AspNetCore.Mvc;
using model;

namespace cp.ViewComponents;

public class DomainIpViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<DomainIp> ipDomainsModel)
    {
        return View(ipDomainsModel);
    }
}