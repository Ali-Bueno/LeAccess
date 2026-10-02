using System;
using EHG.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

internal readonly record struct VitalsSnapshot(long Session, double Health, double MaxHealth,
    double Mana, double MaxMana, bool ManaReplaced);

internal static class VitalsProbe
{
    internal static bool TryRead(out VitalsSnapshot snapshot)
    {
        snapshot = default;
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return false;
        BaseHealth health;
        BaseMana mana;
        UnityEngine.Object player;
        if (GameplayEnvironment.IsOnlineClient)
        {
            var visuals = PlayerFinder.playerActorVisuals;
            var local = PlayerFinder.getLocalPlayerInMultiplayer();
            if (!Available(visuals, scene) || !Available(local, scene)) return false;
            var sync = visuals.actorSync;
            var localSync = local.actorSync;
            if (!IsAlive(sync) || !IsAlive(localSync) || sync.Pointer != localSync.Pointer) return false;
            var playerSync = sync.TryCast<PlayerActorSync>();
            if (!IsAlive(playerSync) || !IsAlive(playerSync.localPlayer) ||
                playerSync.localPlayer.Pointer != local.Pointer) return false;
            player = visuals;
            health = local.playerHealth;
            mana = local.playerMana;
            if (!Owned(health, local.gameObject, scene)) health = null;
            if (!Owned(mana, local.gameObject, scene)) mana = null;
        }
        else
        {
            var actor = PlayerFinder.playerActorComponent;
            if (!Available(actor, scene)) return false;
            player = actor;
            health = actor.health;
            mana = actor.mana;
            if (!Owned(health, actor.gameObject, scene) || !IsAlive(health.actor) ||
                health.actor.Pointer != actor.Pointer) health = null;
            if (!Owned(mana, actor.gameObject, scene)) mana = null;
        }
        double currentHealth = double.NaN, maxHealth = double.NaN;
        double currentMana = double.NaN, maxMana = double.NaN;
        bool replaced = false;
        if (IsAlive(health))
        {
            currentHealth = health.currentHealth;
            maxHealth = health.maxHealth;
        }
        if (IsAlive(mana))
        {
            replaced = mana.manaReplacedByRage();
            if (!replaced)
            {
                currentMana = mana.currentMana;
                maxMana = mana.maxMana;
            }
        }
        if (double.IsNaN(currentHealth) && double.IsNaN(currentMana) && !replaced) return false;
        long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
        snapshot = new VitalsSnapshot(session, currentHealth, maxHealth, currentMana, maxMana, replaced);
        return true;
    }

    private static bool Available(Behaviour component, Scene scene) => IsAlive(component) &&
        component.isActiveAndEnabled && (int)component.gameObject.scene.handle == (int)scene.handle;

    private static bool Owned(Behaviour component, GameObject owner, Scene scene) =>
        Available(component, scene) && component.transform.IsChildOf(owner.transform);
}
