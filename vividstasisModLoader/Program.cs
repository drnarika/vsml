using Microsoft.Win32;
using System.Text.Json;
using UndertaleModLib;
using vividstasisModLoader;

// 解析命令行参数，判断是否为还原模式。
bool IsRestoreMode(string[] inputArgs)
{
    return inputArgs.Length > 0 && inputArgs[0] == "restore";
}

// 生成一键还原脚本，便于用户快速回退。
void CreateRestoreScript()
{
    File.WriteAllText("./restore.cmd", "vividstasisModLoader restore");
}

// 读取配置文件，若不存在或解析失败则返回默认配置。
ModLoaderConfig LoadConfig()
{
    if (!File.Exists("./config.json"))
    {
        return new ModLoaderConfig();
    }

    var config = JsonSerializer.Deserialize<ModLoaderConfig>(File.ReadAllText("./config.json"));
    return config ?? new ModLoaderConfig();
}

// 自动检测游戏目录，检测失败时提示用户手动输入。
string ResolveGamePath()
{
    var gamePath = Registry.GetValue(
        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2093940",
        "InstallLocation",
        null
    ) as string ?? string.Empty;

    if (gamePath == string.Empty || !Directory.Exists(gamePath))
    {
        Console.Error.WriteLine("Couldn't find game path. Please make sure vivid/stasis is installed.");
        Console.Write("Please input the game path: ");
        gamePath = Console.ReadLine() ?? string.Empty;
    }

    return gamePath;
}

// 在还原模式下恢复备份文件并删除备份目录。
bool TryRestoreFromBackup(bool restoreMode, string backupFolderPath, string gamePath)
{
    if (!restoreMode)
    {
        return false;
    }

    if (!Directory.Exists(backupFolderPath))
    {
        Console.Error.WriteLine("Couldn't find backup folder.");
        return true;
    }

    foreach (var file in Directory.GetFiles(backupFolderPath, "*.*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(backupFolderPath, file);
        File.Copy(file, Path.Combine(gamePath, relativePath), true);
        Console.WriteLine($"restored {relativePath}.");
    }

    Directory.Delete(backupFolderPath, true);
    return true;
}

// 根据游戏版本准备备份，并在已有备份时先回滚 data.win 与音频组。
void PrepareBackup(string gamePath, string dataFilePath, string backupFolderPath)
{
    var backupDataPath = Path.Combine(backupFolderPath, "data.win");
    var gameVerPath = Path.Combine(gamePath, "ver");
    var backupVerPath = Path.Combine(backupFolderPath, "ver");

    Directory.CreateDirectory(backupFolderPath);

    var gameVer = File.ReadAllText(gameVerPath);
    if (File.Exists(backupVerPath))
    {
        var backupVer = File.ReadAllText(backupVerPath);
        if (gameVer != backupVer)
        {
            Directory.Delete(backupFolderPath, true);
        }
    }

    Directory.CreateDirectory(backupFolderPath);

    if (File.Exists(backupDataPath))
    {
        File.Copy(backupDataPath, dataFilePath, true);
        foreach (var file in Directory.GetFiles(backupFolderPath, "*.dat"))
        {
            File.Copy(file, Path.Combine(gamePath, Path.GetFileName(file)), true);
        }
        Console.WriteLine("Restored data.win and audiogroups from backup.");
    }
    else
    {
        File.Copy(dataFilePath, backupDataPath);
        foreach (var file in Directory.GetFiles(gamePath, "*.dat"))
        {
            File.Copy(file, Path.Combine(backupFolderPath, Path.GetFileName(file)), true);
        }
        File.Copy(gameVerPath, backupVerPath);
        Console.WriteLine("Backup file created.");
    }
}

// 获取 mods 目录下的所有模组目录。
string[] GetModDirectories()
{
    Directory.CreateDirectory("./mods");
    return Directory.GetDirectories("./mods");
}

// 对每个模组依次执行字体、文本、图片、音频、Shader、对象和代码修补。
void PatchMods(UndertaleData data, string gamePath, string[] modDirs)
{
    foreach (var modDir in modDirs)
    {
        Console.WriteLine("Mod dir: " + modDir);

        Console.WriteLine("Patching Fonts...");
        var fontReplacer = new FontReplacer(data, modDir);
        fontReplacer.Execute();
        Console.WriteLine("Fonts patched.");

        Console.WriteLine("Patching Strings...");
        var strReplacer = new StringReplacer(data, modDir);
        strReplacer.Execute();
        Console.WriteLine("Strings patched.");

        Console.WriteLine("Patching Sprites...");
        var spriteReplacer = new SpriteReplacer(data, modDir);
        spriteReplacer.Execute();
        Console.WriteLine("Sprites patched.");

        Console.WriteLine("Patching Audios...");
        var audioReplacer = new AudioReplacer(data, gamePath, modDir);
        audioReplacer.Execute();
        Console.WriteLine("Audios patched.");

        Console.WriteLine("Patching Shaders...");
        var shaderReplacer = new ShaderReplacer(data, modDir);
        shaderReplacer.Execute();
        Console.WriteLine("Shaders patched.");

        Console.WriteLine("Patching Objects...");
        var objectPatcher = new ObjectPatcher(data, modDir);
        objectPatcher.Execute();
        Console.WriteLine("Objects patched.");

        Console.WriteLine("Patching Codes...");
        var codePatcher = new CodePatcher(data, modDir);
        codePatcher.Execute();
        Console.WriteLine("Codes patched.");
    }
}

// 处理 raw 文件覆盖，并在首次覆盖前写入备份文件。
void PatchRawFiles(string[] modDirs, string gamePath, string backupFolderPath)
{
    foreach (var modDir in modDirs)
    {
        var rawPath = Path.Combine(modDir, "raw");
        if (!Directory.Exists(rawPath))
        {
            continue;
        }

        Console.WriteLine($"Patching raw files for {modDir}...");

        foreach (var rawFile in Directory.GetFiles(rawPath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(rawPath, rawFile);
            var gamePathFile = new FileInfo(Path.Combine(gamePath, relativePath));
            var backupPathFile = new FileInfo(Path.Combine(backupFolderPath, relativePath));

            if (gamePathFile.Exists && !backupPathFile.Exists)
            {
                if (!backupPathFile.Directory!.Exists)
                {
                    Directory.CreateDirectory(backupPathFile.Directory.FullName);
                }

                File.Copy(gamePathFile.FullName, backupPathFile.FullName);
            }

            if (!gamePathFile.Directory!.Exists)
            {
                Directory.CreateDirectory(gamePathFile.Directory.FullName);
            }

            File.Copy(rawFile, gamePathFile.FullName, true);
        }

        Console.WriteLine("Raw files patched.");
    }
}

// 读取 data.win 并解析为 UndertaleData。
UndertaleData ReadDataFile(FileInfo dataFile)
{
    try
    {
        using var fs = dataFile.OpenRead();
        return UndertaleIO.Read(fs);
    }
    catch (FileNotFoundException e)
    {
        throw new FileNotFoundException($"Data file '{e.FileName}' does not exist");
    }
}

// 将修补后的 UndertaleData 写回 data.win。
void SaveDataFile(FileInfo dataFile, UndertaleData data)
{
    try
    {
        using var fs = dataFile.OpenWrite();
        UndertaleIO.Write(fs, data);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e.Message);
    }
}

// 持久化当前配置到 config.json。
void SaveConfig(ModLoaderConfig config)
{
    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("./config.json", json);
}

// 修补完成后暂停，便于查看日志输出。
void PauseAfterPatch()
{
    Console.WriteLine("Mod patching completed. Press Enter to exit...");
    Console.ReadLine();
}

// 统一执行主流程。
void Run(string[] inputArgs)
{
    var restoreMode = IsRestoreMode(inputArgs);
    CreateRestoreScript();

    var config = LoadConfig();
    var gamePath = ResolveGamePath();
    config.GamePath = gamePath;
    Console.WriteLine("Game path: " + gamePath);

    var dataFilePath = Path.Combine(gamePath, "data.win");
    var backupFolderPath = Path.Combine(gamePath, "backup\\");

    if (TryRestoreFromBackup(restoreMode, backupFolderPath, gamePath))
    {
        return;
    }

    PrepareBackup(gamePath, dataFilePath, backupFolderPath);

    var dataFileInfo = new FileInfo(dataFilePath);
    var data = ReadDataFile(dataFileInfo);
    var modDirs = GetModDirectories();

    PatchMods(data, gamePath, modDirs);
    SaveDataFile(dataFileInfo, data);
    PatchRawFiles(modDirs, gamePath, backupFolderPath);
    SaveConfig(config);
    PauseAfterPatch();
}

// 在文件底部统一触发执行。
Run(args);

// 配置对象，保存基础运行参数。
class ModLoaderConfig
{
    public string GamePath { get; set; } = string.Empty;
}

