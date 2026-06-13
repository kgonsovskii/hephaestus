namespace Commons;

public interface IHephaestusPathResolver
{
    string ResolveRepositoryRoot(string startDirectory);

    string ResolveHephaestusDataRoot(string startDirectory);

    string ResolveHephaestusDataRootFromAppBase();

    string WebDirectory(string hephaestusDataRoot);

    string CertDirectory(string repositoryRoot);

    string FileUnderDataRoot(string hephaestusDataRoot);

    string FileUnderCert(string repositoryRoot);

    string PublicCertPath(string repositoryRoot);
}
