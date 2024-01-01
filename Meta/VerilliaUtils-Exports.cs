using MonoMod.ModInterop;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Verillia.Utils {
    /// <summary>
    /// Provides export functions for other mods to import.
    /// If you do not need to export any functions, delete this class and the corresponding call
    /// to ModInterop() in <see cref="Verillia_UtilsModule.Load"/>
    /// </summary>
    [ModExportName("VerilliaUtils")]
    public static class VerilliaUtilsExports {
        // TODO: add your mod's exports, if required
        public static void AddMovementMode(Player player, string name, int StIndex)
        {
            VerilliaUtilsPlayerExt.MovementModes.Add(name, StIndex);
        }
    }
}
