namespace FactorioModManager.App.Factorio;

public sealed class ModListFileManager
{
    public string CreateManagedListFolder(string modsFolderPath, string name)
    {
        var folderPath = ManagerWorkspacePaths.GetManagedListFolder(modsFolderPath, name);
        if (Directory.Exists(folderPath))
        {
            throw new IOException("A folder with this mod-list name already exists.");
        }

        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    public string RenameManagedList(string modsFolderPath, string currentFolderPath, string newName)
    {
        if (!ManagerWorkspacePaths.IsManagedListPath(modsFolderPath, currentFolderPath) ||
            !ModListDetector.IsManagedListFolder(currentFolderPath))
        {
            throw new InvalidOperationException("Only recognized managed mod-list folders can be renamed.");
        }

        var destination = ManagerWorkspacePaths.GetManagedListFolder(modsFolderPath, newName);
        if (Directory.Exists(destination))
        {
            throw new IOException("A folder with this mod-list name already exists.");
        }

        Directory.Move(currentFolderPath, destination);
        return destination;
    }

    public void DeleteManagedList(string modsFolderPath, string folderPath)
    {
        if (!ManagerWorkspacePaths.IsManagedListPath(modsFolderPath, folderPath) ||
            !ModListDetector.IsManagedListFolder(folderPath))
        {
            throw new InvalidOperationException("Only recognized managed mod-list folders can be deleted.");
        }

        Directory.Delete(folderPath, recursive: true);
    }

    public void ApplyRootFilesToManagedList(string modsFolderPath, string folderPath)
    {
        if (!ManagerWorkspacePaths.IsManagedListPath(modsFolderPath, folderPath) ||
            !ModListDetector.IsManagedListFolder(folderPath))
        {
            throw new InvalidOperationException("Only recognized managed mod-list folders can be updated.");
        }

        var rootModList = Path.Combine(modsFolderPath, FactorioFileNames.ModListJson);
        var rootModSettings = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);
        if (!File.Exists(rootModList))
        {
            throw new FileNotFoundException("Root mod-list.json is missing.", rootModList);
        }

        if (!File.Exists(rootModSettings))
        {
            throw new FileNotFoundException("Root mod-settings.dat is missing.", rootModSettings);
        }

        File.Copy(rootModList, Path.Combine(folderPath, FactorioFileNames.ModListJson), overwrite: true);
        File.Copy(rootModSettings, Path.Combine(folderPath, FactorioFileNames.ModSettingsDat), overwrite: true);
    }
}
