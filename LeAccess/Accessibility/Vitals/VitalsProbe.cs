using System;
using EHG.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

internal readonly record struct VitalsSnapshot(long Session, double Health, double MaxHealth,
    double Mana, double MaxMana, bool ManaReplaced, int Potions = -1, int MaxPotions = -1);

internal static class VitalsProbe
{
    internal static bool TryRead(out VitalsSnapshot snapshot)
    {
        snapshot = default;
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return false;
        BaseHealth health;
        BaseMana mana;
        HealthPotionCharges potion;
        UnityEngine.Object player;
        if (GameplayEnvironment.IsOnlineClient)
        {
            var visuals = LocalPlayerSource.Visuals;
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
            potion = local.healthPotion;
            if (!Owned(health, local.gameObject, scene)) health = null;
            if (!Owned(mana, local.gameObject, scene)) mana = null;
            if (!Belongs(potion, local.gameObject, scene)) potion = null;
        }
        else
        {
            var actor = PlayerFinder.playerActorComponent;
            if (!Available(actor, scene)) return false;
            player = actor;
            health = actor.health;
            mana = actor.mana;
            potion = actor.healthPotion;
            if (!Belongs(potion, actor.gameObject, scene)) potion = null;
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
        int potions = -1, maxPotions = -1;
        if (IsAlive(potion) && potion.currentCharges >= 0)
        {
            potions = potion.currentCharges;
            maxPotions = potion.maxCharges > 0 ? potion.maxCharges : -1;
        }
        long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
        snapshot = new VitalsSnapshot(session, currentHealth, maxHealth, currentMana, maxMana, replaced, potions, maxPotions);
        return true;
    }

    private static bool Available(Behaviour component, Scene scene) => IsAlive(component) &&
        component.isActiveAndEnabled && (int)component.gameObject.scene.handle == (int)scene.handle;

    private static bool Owned(Behaviour component, GameObject owner, Scene scene) =>
        Available(component, scene) && component.transform.IsChildOf(owner.transform);

    // Potion charges are data on the player; the component need not be enabled to hold them.
    private static bool Belongs(Component component, GameObject owner, Scene scene) => IsAlive(component) &&
        (int)component.gameObject.scene.handle == (int)scene.handle && component.transform.IsChildOf(owner.transform);
}
