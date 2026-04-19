namespace Commons;

public interface IHephaestusPathResolver
{
    string ResolveRepositoryRoot(string startDirectory);

    string ResolveHephaestusDataRoot(string startDirectory);

    string ResolveHephaestusDataRootFromAppBase();

    string WebDirectory(string hephaestusDataRoot);

    string CertDirectory(string hephaestusDataRoot);

    string FileUnderDataRoot(string hephaestusDataRoot);

    string FileUnderCert(string hephaestusDataRoot);

    string PublicCertPath(string hephaestusDataRoot);
}
