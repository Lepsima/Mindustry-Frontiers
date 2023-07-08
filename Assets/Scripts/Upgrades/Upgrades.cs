using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontiers.Content.Upgrades {

    public enum UpgradeClass {

    }

    public static class UpgradeResearcher {
        public static List<UpgradeType> researchedUpgrades = new();

        public static bool IsResearched(UpgradeType upgradeType) {
            return researchedUpgrades.Contains(upgradeType);
        }

        public static bool IsResearched(UpgradeType[] upgradeTypes) {
            foreach (UpgradeType upgradeType in upgradeTypes) if (!IsResearched(upgradeType)) return false;
            return true;
        }
    }


    public static class UpgradeHandler {

        public static Dictionary<short, UpgradeType> loadedUpgrades = new();

 

        public static void HandleUpgrade(UpgradeType upgradeType) {
            if (GetUpgradeByName(upgradeType.name) != null) throw new ArgumentException("Two upgrades cannot have the same name! (issue: '" + upgradeType.name + "')");
            loadedUpgrades.Add(upgradeType.id, upgradeType);
        }

        public static UpgradeType GetUpgradeByName(string name) {
            foreach (UpgradeType upgradeType in loadedUpgrades.Values) if (upgradeType.name == name) return upgradeType;
            return null;
        }
    }

    public abstract class UpgradeType {
        public string name;
        public short id;

        public bool isUnlocked;
        public int minTier = 1, maxTier = 1;

        public ItemStack[] installCost;
        public ItemStack[] researchCost;

        public UpgradeType[] previousUpgrades;
        public UpgradeType[] nextUpgrades;

        public UpgradeMultipliers properties;

        public UpgradeType(string name) {
            id = (short)UpgradeHandler.loadedUpgrades.Count;
            if (name == null) this.name = "upgrade " + id;

            UpgradeHandler.HandleUpgrade(this);
        }

        public virtual bool CanBeResearched() {
            bool arePreviousResearched = UpgradeResearcher.IsResearched(previousUpgrades);
            return arePreviousResearched;
        }

        public virtual bool CompatibleWith(EntityType entityType) {
            bool tierPass = entityType.tier >= minTier && entityType.tier <= maxTier;
            return tierPass;
        }

        public virtual void ApplyUpgrade(Entity entity) {

        }
    }

    public class UpgradeMultipliers {
        public float mainMultiplier = 1f;
        public float entity_health, entity_itemCapacity;
    }

    public class BlockUpgradeMultipliers : UpgradeMultipliers {
        public float drill_hardness, drill_rate;
        public float crafter_craftTime, crafter_craftCost, crafter_craftReturn;
        public float conveyor_itemSpeed;
    }

    public class UnitUpgradeMultipliers : UpgradeMultipliers {
        public float unit_maxVelocity, unit_rotationSpeed;
        public float unit_itemPickupDistance, unit_buildSpeedMultiplier;
        public float unit_range, unit_searchRange, unit_fov;
        public float unit_fuelCapacity, unit_fuelConsumption, unit_fuelRefillRate;
        public float unit_emptyMass, unit_fuelMass;

        public float mech_baseRotationSpeed;

        public float flying_drag, flying_force;
        public float flying_takeoffTime, flying_takeoffHeight;
        public float flying_maxLiftVelocity;
    }
}