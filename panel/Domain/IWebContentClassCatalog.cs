namespace Domain;

/// <summary>Lists subfolder names under <c>web/classes</c> for control-panel comboboxes.</summary>
public interface IWebContentClassCatalog
{
    /// <summary>Folder names under <c>web/classes</c> (e.g. <c>analytics</c>), sorted; always includes <see cref="WebSiteLayout.DefaultDomainClassFolderName"/>.</summary>
    IReadOnlyList<string> ListClassFolderNames();
}
