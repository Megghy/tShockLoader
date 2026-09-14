using Terraria;
using Terraria.DataStructures;

namespace tShockLoader.HookAttach;

public static class NPCHooks
{
    public static void Attach()
    {
        On_NPC.Transform += OnTransform;
        On_NPC.NewNPC += OnNewNPC;
        On_NPC.SpawnOnPlayer += OnSpawnOnPlayer;
    }

    public static void Detach()
    {
        On_NPC.Transform -= OnTransform;
        On_NPC.NewNPC -= OnNewNPC;
        On_NPC.SpawnOnPlayer -= OnSpawnOnPlayer;
    }

    static void OnTransform(On_NPC.orig_Transform orig, NPC self, int newType)
    {
        if (OTAPI.Hooks.NPC.InvokeTransforming(self, ref newType))
            orig(self, newType);
    }

    static int OnNewNPC(On_NPC.orig_NewNPC orig, IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
    {
        if (OTAPI.Hooks.NPC.InvokeCreate(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target) is null)
            return 0;
        return orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
    }

    static void OnSpawnOnPlayer(On_NPC.orig_SpawnOnPlayer orig, int plr, int type)
    {
        var index = plr;
        if (OTAPI.Hooks.NPC.InvokeSpawn(ref index))
            orig(index, type);
    }
}
