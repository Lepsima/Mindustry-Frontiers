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

    public static class Upgrades {
        public static UpgradeType a, b, c;

        public static void Load() {
            a = new UpgradeType("Heavy fighter armor - Tier I") {
                minTier = 1,
                maxTier = 1,
            };
        }
    }

    public class UpgradeType {
        public string name;
        public short id;

        public int tier = 1;
        public bool isUnlocked;

        public int minTier = 1, maxTier = 1;
        public string[] compatibleFlags;

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
            bool tierPass = (minTier == -1 || entityType.tier >= minTier) && (maxTier == -1 || entityType.tier <= maxTier);
            bool hasFlags = entityType.HasFlags(compatibleFlags);
            return tierPass && hasFlags;
        }
    }

    public abstract class UpgradeMultipliers {
        public abstract void ApplyMult(float mult);

        public abstract void CopyTo(UpgradeMultipliers upgradeMultipliers);
    }

    public class WeaponUpgradeMultipliers : UpgradeMultipliers {
        public override void ApplyMult(float mult) {

        }

        public override void CopyTo(UpgradeMultipliers upgradeMultipliers) {
            WeaponUpgradeMultipliers mult = upgradeMultipliers as WeaponUpgradeMultipliers;
        }
    }

    public class EntityUpgradeMultipliers : UpgradeMultipliers {
        public float entity_health, entity_itemCapacity;

        public override void ApplyMult(float mult) {
            entity_health *= mult;
            entity_itemCapacity *= mult;
        }

        public override void CopyTo(UpgradeMultipliers upgradeMultipliers) {
            EntityUpgradeMultipliers mult = upgradeMultipliers as EntityUpgradeMultipliers;

            mult.entity_health = entity_health;
            mult.entity_itemCapacity = entity_itemCapacity;
        }
    }

    public class BlockUpgradeMultipliers : EntityUpgradeMultipliers {
        public float drill_hardness, drill_rate;
        public float crafter_craftTime, crafter_craftCost, crafter_craftReturn;
        public float conveyor_itemSpeed;

        public override void ApplyMult(float mult) {
            base.ApplyMult(mult);

            drill_hardness *= mult;
            drill_rate *= mult;
            crafter_craftTime *= mult;
            crafter_craftCost *= mult;
            crafter_craftReturn *= mult;
            conveyor_itemSpeed *= mult;
        }

        public override void CopyTo(UpgradeMultipliers upgradeMultipliers) {
            BlockUpgradeMultipliers mult = upgradeMultipliers as BlockUpgradeMultipliers;
            base.CopyTo(mult);

            mult.drill_hardness = drill_hardness;
            mult.drill_rate = drill_rate;
            mult.crafter_craftTime = crafter_craftTime;
            mult.crafter_craftReturn = crafter_craftReturn;
            mult.crafter_craftCost = crafter_craftCost;
            mult.conveyor_itemSpeed = conveyor_itemSpeed;
        }
    }

    public class UnitUpgradeMultipliers : EntityUpgradeMultipliers {
        public float unit_maxVelocity, unit_rotationSpeed;
        public float unit_itemPickupDistance, unit_buildSpeedMultiplier;
        public float unit_range, unit_searchRange, unit_fov;
        public float unit_fuelCapacity, unit_fuelConsumption, unit_fuelRefillRate;
        public float unit_emptyMass, unit_fuelMass;

        public float mech_baseRotationSpeed;

        public float flying_drag, flying_force;
        public float flying_takeoffTime, flying_takeoffHeight;
        public float flying_maxLiftVelocity;

        public override void ApplyMult(float mult) {
            base.ApplyMult(mult);

            unit_maxVelocity *= mult;
            unit_rotationSpeed *= mult;
            unit_itemPickupDistance *= mult;
            unit_buildSpeedMultiplier *= mult;
            unit_range *= mult;
            unit_searchRange *= mult;
            unit_fov *= mult;
            unit_fuelCapacity *= mult;
            unit_fuelConsumption *= mult;
            unit_fuelRefillRate *= mult;
            unit_emptyMass *= mult;
            unit_fuelMass *= mult;

            mech_baseRotationSpeed *= mult;

            flying_drag *= mult;
            flying_force *= mult;
            flying_takeoffTime *= mult;
            flying_takeoffHeight *= mult;
            flying_maxLiftVelocity *= mult;
        }

        public override void CopyTo(UpgradeMultipliers upgradeMultipliers) {
            UnitUpgradeMultipliers mult = upgradeMultipliers as UnitUpgradeMultipliers;
            base.CopyTo(mult);

            mult.unit_maxVelocity = unit_maxVelocity;
            mult.unit_rotationSpeed = unit_rotationSpeed;
            mult.unit_itemPickupDistance = unit_itemPickupDistance;
            mult.unit_buildSpeedMultiplier = unit_buildSpeedMultiplier;
            mult.unit_range = unit_range;
            mult.unit_searchRange = unit_searchRange;
            mult.unit_fov = unit_fov;
            mult.unit_fuelCapacity = unit_fuelCapacity;
            mult.unit_fuelConsumption = unit_fuelConsumption;
            mult.unit_fuelRefillRate = unit_fuelRefillRate;
            mult.unit_emptyMass = unit_emptyMass;
            mult.unit_fuelMass = unit_fuelMass;

            mult.mech_baseRotationSpeed = mech_baseRotationSpeed;

            mult.flying_drag = flying_drag;
            mult.flying_force = flying_force;
            mult.flying_takeoffTime = flying_takeoffTime;
            mult.flying_takeoffHeight = flying_takeoffHeight;
            mult.flying_maxLiftVelocity = flying_maxLiftVelocity;
        }
    }

}