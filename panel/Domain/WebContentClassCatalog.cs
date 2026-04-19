namespace Domain;

public sealed class WebContentClassCatalog : IWebContentClassCatalog
{
    private readonly IWebContentPathProvider _paths;

    public WebContentClassCatalog(IWebContentPathProvider paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<string> ListClassFolderNames()
    {
        var web = _paths.WebRootFullPath;
        var sitesDir = Path.Combine(web, WebSiteLayout.SitesFolderName);
        var classesDir = Path.Combine(web, WebSiteLayout.ClassesFolderName);
        try
        {
            if (!Directory.Exists(sitesDir))
                Directory.CreateDirectory(sitesDir);
            if (!Directory.Exists(classesDir))
                Directory.CreateDirectory(classesDir);
        }
        catch
        {
            
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            WebSiteLayout.DefaultDomainClassFolderName
        };

        try
        {
            if (Directory.Exists(classesDir))
            {
                foreach (var path in Directory.EnumerateDirectories(classesDir))
                {
                    var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!string.IsNullOrEmpty(name) && name is not "." and not "..")
                        set.Add(name);
                }
            }
        }
        catch
        {
        }

        var list = set.ToList();
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }
}
