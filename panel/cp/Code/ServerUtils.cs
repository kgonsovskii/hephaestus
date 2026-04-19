namespace cp.Code;

public static class ServerUtils
{
    public static void DeleteFolderRecursive(string folderPath)
    {
        // Check if the folder exists
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' not found.");
        }

        // Delete all files and subdirectories recursively
        foreach (string file in Directory.GetFiles(folderPath))
        {
            File.Delete(file);
        }

        foreach (string subdirectory in Directory.GetDirectories(folderPath))
        {
            DeleteFolderRecursive(subdirectory); // Recursively delete subdirectories
        }

        // Finally, delete the empty folder
        Directory.Delete(folderPath);
    }
}