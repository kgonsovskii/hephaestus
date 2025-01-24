
namespace model;

public class PsList : PsBase
{
    public PsList(ServerModel serverModel) : base(serverModel)
    {
    }

    public override List<string> Run(params (string Name, object Value)[] parameters)
    {
        return ExecuteRemoteScript("list", parameters);
    }
}