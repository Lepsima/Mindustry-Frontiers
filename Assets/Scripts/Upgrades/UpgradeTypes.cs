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
        public static Dictionary<string, UpgradeType> loadedUpgrades = new();

        public static void HandleUpgrade(UpgradeType upgradeType) {
            if (GetUpgradeByName(upgradeType.name) != null) throw new ArgumentException("Two upgrades cannot have the same name! (issue: '" + upgradeType.name + "')");
            loadedUpgrades.Add(upgradeType.name, upgradeType);
        }

        public static UpgradeType[] GetUpgradesByName(string[] names) {
            UpgradeType[] upgrades = new UpgradeType[names.Length];
            for (int i = 0; i < names.Length; i++) upgrades[i] = GetUpgradeByName(names[i]);
            return upgrades;
        }

        public static UpgradeType GetUpgradeByName(string name) {
            return loadedUpgrades[name];
        }
    }

    public static class UpgradeTypes {
        public static UpgradeType[] heavyFighterArmor;
        public static UpgradeType[] fuelEfficiency;

        /*
            heavyFighterArmor[0] = new UpgradeType("upgradeTest2") {
                displayName = "Upgrade Test - Tier II",
                tier = 2,
                compatibleFlags = new string[] { "light" },

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrades = new string[] { "heavyFighterArmor1" },

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.1f,
                    unit_emptyMass = 0.05f,
                }
            };   
         */
        public static void Load() {
            heavyFighterArmor = new UpgradeType[3];
            heavyFighterArmor[0] = new UpgradeType("heavyFighterArmor1") {
                displayName = "Heavy fighter armor - Tier I",
                tier = 1,
                compatibleFlags = new string[] { "fighter" },

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrades = null,

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.1f,
                    unit_emptyMass = 0.05f,
                    unit_maxVelocity = -0.075f,
                }
            };
            heavyFighterArmor[1] = new UpgradeType("heavyFighterArmor2") {
                displayName = "Heavy fighter armor - Tier II",
                tier = 2,
                compatibleFlags = new string[] { "fighter" },

                installCost = ItemStack.With(Items.copper, 10, Items.silicon, 5),
                researchCost = ItemStack.With(Items.copper, 125, Items.silicon, 45),

                previousUpgrades = new string[] { "heavyFighterArmor1" },

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.15f,
                    unit_emptyMass = 0.1f,
                    unit_maxVelocity = -0.1f,
                }
            };
            heavyFighterArmor[2] = new UpgradeType("heavyFighterArmor3") {
                displayName = "Heavy fighter armor - Tier III",
                tier = 3,
                compatibleFlags = new string[] { "fighter" },

                installCost = ItemStack.With(Items.silicon, 12, Items.titanium, 5),
                researchCost = ItemStack.With(Items.silicon, 160, Items.titanium, 35),

                previousUpgrades = new string[] { "heavyFighterArmor2" },

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.225f,
                    unit_emptyMass = 0.15f,
                    unit_maxVelocity = -0.125f,
                }
            };

            fuelEfficiency = new UpgradeType[2];

            fuelEfficiency[0] = new UpgradeType("fuelEfficiency1") {
                displayName = "Fuel Efficiency - Tier I",
                tier = 1,
                compatibleFlags = new string[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrades = null,

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelConsumption = -0.075f,
                    unit_fuelCapacity = -0.05f,
                    unit_fuelDensity = 0.05f,
                }
            };
            fuelEfficiency[1] = new UpgradeType("fuelEfficiency2") {
                displayName = "Fuel Efficiency - Tier II",
                tier = 2,
                compatibleFlags = new string[0],

                installCost = ItemStack.With(Items.lead, 15, Items.metaglass, 5),
                researchCost = ItemStack.With(Items.silicon, 50, Items.metaglass, 50),

                previousUpgrades = new string[] { "fuelEfficiency1" },

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelConsumption = -0.15f,
                    unit_fuelCapacity = -0.125f,
                    unit_fuelDensity = 0.1f,
                }
            };
        }
    }

    public class UpgradeType {
        public string name;
        public string displayName;
        public short id;

        public int tier = 1;
        public bool isUnlocked;

        public int minTier = -1, maxTier = -1;
        public string[] compatibleFlags;

        public ItemStack[] installCost;
        public ItemStack[] researchCost;

        public string[] previousUpgrades;

        public UpgradeMultipliers properties;

        public UpgradeType(string name) {
            this.name = name;

            id = (short)UpgradeHandler.loadedUpgrades.Count;
            if (name == null) this.name = "upgrade " + id;

            UpgradeHandler.HandleUpgrade(this);
        }

        public virtual bool CanBeResearched() {
            if (previousUpgrades == null || previousUpgrades.Length == 0) return true;

            UpgradeType[] previous = UpgradeHandler.GetUpgradesByName(previousUpgrades);
            bool arePreviousResearched = UpgradeResearcher.IsResearched(previous);

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
        public float unit_emptyMass, unit_fuelDensity;

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
            unit_fuelDensity *= mult;

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
            mult.unit_fuelDensity = unit_fuelDensity;

            mult.mech_baseRotationSpeed = mech_baseRotationSpeed;

            mult.flying_drag = flying_drag;
            mult.flying_force = flying_force;
            mult.flying_takeoffTime = flying_takeoffTime;
            mult.flying_takeoffHeight = flying_takeoffHeight;
            mult.flying_maxLiftVelocity = flying_maxLiftVelocity;
        }
    }

}