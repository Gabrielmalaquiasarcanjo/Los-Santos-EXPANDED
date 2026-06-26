
using Rage;
using System;

[assembly: Rage.Attributes.Plugin("[LSE] Alpha Teste build v0.001}", Description = "This is a alpha test in currently in development.", Author = "Geekarcanjo")]

namespace LosSantosExpanded
{
    public static class EntryPoint
    {
        public static void Main()
        {
            Game.LogTrivial("[LSE] Starting...");

            ModController.Instance.Initialize();
            ModController.Instance.Run();

            Game.LogTrivial("[LSE] Shutdown.");
        }
    }
}
