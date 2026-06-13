namespace Commons;

public interface IHephaestusPathResolver
{
    void EnsureDirectories(string startDirectory);

    void EnsureDirectoriesFromAppBase();

    string ResolveRepositoryRoot(string startDirectory);

    string ResolveRepositoryRootFromAppBase();

    string ResolveHephaestusDataBase(string startDirectory);

    string ResolveHephaestusDataRoot(string startDirectory);

    string ResolveHephaestusDataRootFromAppBase();

    string ProfileDirectory(string startDirectory);

    string ProfileDirectoryFromAppBase();

    string DefaultsEmbedDirectory(string startDirectory);

    string WebDirectory(string profileRoot);

    string CertDirectory(string repositoryRoot);

    string FileUnderDataRoot(string profileRoot);

    string FileUnderCert(string repositoryRoot);

    string PublicCertPath(string repositoryRoot);
}
