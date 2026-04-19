namespace cp.Code;

public static class ServerUtils
{
    public static void DeleteFolderRecursive(string folderPath)
    {
        
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' not found.");
        }

        
        foreach (string file in Directory.GetFiles(folderPath))
        {
            File.Delete(file);
        }

        foreach (string subdirectory in Directory.GetDirectories(folderPath))
        {
            DeleteFolderRecursive(subdirectory); 
        }

        
        Directory.Delete(folderPath);
    }
}
