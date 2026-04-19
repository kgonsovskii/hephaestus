namespace Domain;

/// <summary>Static files under <see cref="IWebContentPathProvider.WebRootFullPath"/> use <c>sites/{domain}</c> first, then <c>classes/{class}</c>.</summary>
public static class WebSiteLayout
{
    public const string SitesFolderName = "sites";

    public const string ClassesFolderName = "classes";

    /// <summary>When <see cref="Models.DomainRecord.DomainClass"/> is null or empty, class fallback uses this folder under <c>classes/</c>.</summary>
    public const string DefaultDomainClassFolderName = "analytics";
}
