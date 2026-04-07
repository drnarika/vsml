using Microsoft.Win32;
using System.Text.Encodings.Web;
using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;
using vividstasisModLoader;

var restoreMode = false;

if (args.Length > 0)
{
    if (args[0] == "restore")
    {
        restoreMode = true;
    }
}

File.WriteAllText("./restore.cmd", "vividstasisModLoader restore");

var config = new ModLoaderConfig();

if (File.Exists("./config.json"))
{
    config = JsonSerializer.Deserialize<ModLoaderConfig>(File.ReadAllText("./config.json"));
}

var gamePath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2093940", "InstallLocation", null) as string ?? string.Empty;
if (gamePath == string.Empty || !Directory.Exists(gamePath))
{
    Console.Error.WriteLine("Couldn't find game path. Please make sure vivid/stasis is installed.");
    Console.Write("Please input the game path: ");
    gamePath = Console.ReadLine();
}
config.GamePath = gamePath;
Console.WriteLine("Game path: " + gamePath);

var dataFilePath = Path.Combine(gamePath, "data.win");
var backupFolderPath = Path.Combine(gamePath, "backup\\");

if (restoreMode)
{
    if (!Directory.Exists(backupFolderPath))
    {
        Console.Error.WriteLine("Couldn't find backup folder.");
        return;
    }
    foreach (var file in Directory.GetFiles(backupFolderPath, "*.*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(backupFolderPath, file);
        File.Copy(file, Path.Combine(gamePath, relativePath), true);
        Console.WriteLine($"restored {relativePath}.");
    }
    Directory.Delete(backupFolderPath, true);
    return;
}

//创建当前版本的备份文件
var backupDataPath = Path.Combine(backupFolderPath, "data.win");
Directory.CreateDirectory(backupFolderPath);

var gameVerPath = Path.Combine(gamePath, "ver");
var gameVer = File.ReadAllText(gameVerPath);
var backupVerPath = Path.Combine(backupFolderPath, "ver");
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

var dataFileInfo = new FileInfo(dataFilePath);

var data = ReadDataFile(dataFileInfo);

Directory.CreateDirectory("./mods");
var modDirs = Directory.GetDirectories("./mods");
foreach (var modDir in modDirs)
{
    Console.WriteLine("Mod dir: " + modDir);
    //进行字体替换
    Console.WriteLine("Patching Fonts...");
    var fontReplacer = new FontReplacer(data, modDir);

    fontReplacer.Execute();
    Console.WriteLine("Fonts patched.");

    //进行简单String替换
    Console.WriteLine("Patching Strings...");
    var strReplacer = new StringReplacer(data, modDir);

    strReplacer.Execute();
    Console.WriteLine("Strings patched.");

    //进行图片替换
    Console.WriteLine("Patching Sprites...");
    var spriteReplacer = new SpriteReplacer(data, modDir);

    spriteReplacer.Execute();
    Console.WriteLine("Sprites patched.");

    //进行音频替换
    Console.WriteLine("Patching Audios...");
    var audioReplacer = new AudioReplacer(data, gamePath, modDir);

    audioReplacer.Execute();
    Console.WriteLine("Audios patched.");

    //进行Shader修改
    Console.WriteLine("Patching Shaders...");
    var shaderReplacer = new ShaderReplacer(data, modDir);

    shaderReplacer.Execute();
    Console.WriteLine("Shaders patched.");

    //进行Obj修改
    Console.WriteLine("Patching Objects...");
    var objectPatcher = new ObjectPatcher(data, modDir);
    
    objectPatcher.Execute();
    Console.WriteLine("Objects patched.");
    
    //进行代码修改
    Console.WriteLine("Patching Codes...");
    var codePatcher = new CodePatcher(data, modDir);

    codePatcher.Execute();
    Console.WriteLine("Codes patched.");
}

//保存文件
SaveDataFile(dataFileInfo, data);

foreach (var modDir in modDirs)
{
    var rawPath = $"{modDir}/raw";
    if (!Directory.Exists(rawPath)) continue;
    //替换原始文件
    Console.WriteLine($"Patching raw files for {modDir}...");
    foreach (var rawFile in Directory.GetFiles(rawPath, "*.*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(rawPath, rawFile);
        //备份原始文件
        var gamePathFile = new FileInfo(Path.Combine(gamePath, relativePath));
        var backupPathFile = new FileInfo(Path.Combine(backupFolderPath, relativePath));

        if (gamePathFile.Exists)
        {
            if (!backupPathFile.Exists)
            {
                if (!backupPathFile.Directory!.Exists) Directory.CreateDirectory(backupPathFile.Directory.FullName);
                File.Copy(gamePathFile.FullName, backupPathFile.FullName);
            }
        }

        if (!gamePathFile.Directory!.Exists) Directory.CreateDirectory(gamePathFile.Directory.FullName);
        File.Copy(rawFile, gamePathFile.FullName, true);
    }
    Console.WriteLine("Raw files patched.");
}

//保存设置
SaveConfig(config);

return;

static UndertaleData ReadDataFile(FileInfo datafile)
{
    try
    {
        using var fs = datafile.OpenRead();
        var gmData = UndertaleIO.Read(fs);
        return gmData;
    }
    catch (FileNotFoundException e)
    {
        throw new FileNotFoundException($"Data file '{e.FileName}' does not exist");
    }
}

static void SaveDataFile(FileInfo datafile, UndertaleData data)
{
    try
    {
        using var fs = datafile.OpenWrite();
        UndertaleIO.Write(fs, data);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e.Message);
    }
}

void SaveConfig(ModLoaderConfig config)
{
    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("./config.json", json);
}

class ModLoaderConfig
{
    public string GamePath { get; set; }
}