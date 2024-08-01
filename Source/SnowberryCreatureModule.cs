using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using EditorScene = Snowberry.Editor.Editor;

namespace Celeste.Mod.SnowberryCreature;

public class SnowberryCreatureModule : EverestModule
{
    public static SnowberryCreatureModule Instance { get; private set; }

    public SnowberryCreatureModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(SnowberryCreatureModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(SnowberryCreatureModule), LogLevel.Info);
#endif
    }

    private static Hook hook_Editor_PostBeginContent;

    public override void Load()
    {
        hook_Editor_PostBeginContent = new(
            typeof(EditorScene).GetMethod("PostBeginContent", BindingFlags.Instance | BindingFlags.NonPublic),
            on_Editor_PostBeginContent
        );
    }

    public override void Unload()
    {
        hook_Editor_PostBeginContent?.Dispose();
    }

    public static void on_Editor_PostBeginContent(Action<EditorScene> orig, EditorScene editor)
    {
        orig(editor);

        editor.Overlay.Add(new UICreature());
    }
}
