using Spectre.Console;

namespace vividstasisModLoader;

/// <summary>
/// 统一管理控制台双语输出与样式渲染。
/// </summary>
internal static class ConsoleOutput
{
    /// <summary>
    /// 对文本进行转义，避免 Spectre Markup 特殊字符导致渲染异常。
    /// </summary>
    private static string EscapeMarkup(string text)
    {
        return Markup.Escape(text);
    }

    /// <summary>
    /// 输出双语的信息提示。
    /// </summary>
    internal static void PrintInfo(string zh, string en)
    {
        AnsiConsole.MarkupLine($"[cyan]信息[/][white] {EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]");
    }

    /// <summary>
    /// 输出双语的步骤提示。
    /// </summary>
    internal static void PrintStep(string zh, string en)
    {
        AnsiConsole.MarkupLine($"[deepskyblue2]步骤[/][white] {EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]");
    }

    /// <summary>
    /// 输出双语的成功提示。
    /// </summary>
    internal static void PrintSuccess(string zh, string en)
    {
        AnsiConsole.MarkupLine($"[green]成功[/][white] {EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]");
    }

    /// <summary>
    /// 输出双语的警告提示。
    /// </summary>
    internal static void PrintWarning(string zh, string en)
    {
        AnsiConsole.MarkupLine($"[yellow]警告[/][white] {EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]");
    }

    /// <summary>
    /// 输出双语的错误提示。
    /// </summary>
    internal static void PrintError(string zh, string en)
    {
        AnsiConsole.MarkupLine($"[red]错误[/][white] {EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]");
    }

    /// <summary>
    /// 输出分节标题，提升流程可读性。
    /// </summary>
    internal static void PrintSection(string zh, string en)
    {
        var title = $"[bold orange1]{EscapeMarkup(zh)}[/] [grey]({EscapeMarkup(en)})[/]";
        AnsiConsole.Write(new Rule(title));
    }

    /// <summary>
    /// 读取双语提示下的游戏路径输入。
    /// </summary>
    internal static string AskGamePath()
    {
        return AnsiConsole.Ask<string>("[yellow]请输入游戏路径 / Please input the game path:[/]");
    }

    /// <summary>
    /// 输出还原模式完成提示。
    /// </summary>
    internal static void PrintRestoreModeCompleted()
    {
        AnsiConsole.MarkupLine("[green]还原模式已完成。[/] [grey](Restore mode completed.)[/]");
    }

    /// <summary>
    /// 输出结束提示并等待用户按下回车。
    /// </summary>
    internal static void PrintPauseHint()
    {
        AnsiConsole.MarkupLine("[green]修补完成，按 Enter 退出。[/] [grey](Patching completed, press Enter to exit.)[/]");
        Console.ReadLine();
    }
}
