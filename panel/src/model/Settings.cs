namespace model;

public static class Settings
{
    private static string CertKey =
        "cup4sCBQ3NGPKDJEJC0zGzMTrwWPKE9AJxFVH5JXaY3C2WxRy1F0UAlqgC_Eok5HSoScolN0IT34IOB11_tap_buhtig";
        
    public static string CertRepo =
        $"https://kgonsovsky:{r(CertKey)}@github.com/kgonsovskii/cert.git";
    
    static string r(string input)
    {
        var chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}