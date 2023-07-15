using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Flags;

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

    
    // All upgrades are still listed in the main contentLoader, but also here for faster searching mid-game, since there will be a lot of upgrade searchs to check if "x" upgrade can be researched  
    // Maybe is completely unecessary
    public static class UpgradeHandler {
        public static Dictionary<string, UpgradeType> loadedUpgrades = new();

        public static void Handle(UpgradeType upgradeType) {
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
        public static UpgradeType[] 
            heavyArmor, wallDurability, heavyFighterArmor, lightArmor, 
            fuelEfficiency, fuelCapacity, fuelDensity;

        // Upgrade example:
        /*
            heavyFighterArmor[0] = new UpgradeType("upgradeTest2") {
                displayName = "Upgrade Test - Tier II",
                tier = 2,
                flags = new Flag[] { FlagTypes.light },
                incompatibleFlags = new Flag[] { FlagTypes.heavyArmored },

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
            // Health upgrades for any unit
            heavyArmor = new UpgradeType[3];
            heavyArmor[0] = new UpgradeType("heavyArmor1") {
                displayName = "Heavy Armor - Tier I",
                tier = 1,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = null,

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.1f,
                    unit_emptyMass = 0.1f,
                    unit_maxVelocity = -0.1f,
                }
            };
            heavyArmor[1] = new UpgradeType("heavyArmor2") {
                displayName = "Heavy Armor - Tier II",
                tier = 2,
                maxTier = 3,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = heavyArmor[0],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.2f,
                    unit_emptyMass = 0.15f,
                    unit_maxVelocity = -0.175f,
                }
            };
            heavyArmor[2] = new UpgradeType("heavyArmor3") {
                displayName = "Heavy Armor - Tier III",
                tier = 3,
                maxTier = 2,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = heavyArmor[1],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.275f,
                    unit_emptyMass = 0.2f,
                    unit_maxVelocity = -0.225f,
                }
            };

            // Increases wall health, each upgrade is compatible wich it's respective wall tier
            wallDurability = new UpgradeType[4];
            wallDurability[0] = new UpgradeType("wallDurability1") {
                displayName = "Wall Durability - Tier I",
                tier = 1,
                maxTier = 1,

                flags = new Flag[] { FlagTypes.wall },
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.copper, 6),
                researchCost = ItemStack.With(Items.copper, 120),

                previousUpgrade = heavyArmor[0],

                properties = new BlockUpgradeMultipliers() {
                    entity_health = 0.2f,
                }
            };
            wallDurability[1] = new UpgradeType("wallDurability2") {
                displayName = "Wall Durability - Tier II",
                tier = 2,
                minTier = 2,
                maxTier = 2,

                flags = new Flag[] { FlagTypes.wall },
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.titanium, 6),
                researchCost = ItemStack.With(Items.titanium, 120),

                previousUpgrade = wallDurability[0],

                properties = new BlockUpgradeMultipliers() {
                    entity_health = 0.2f,
                }
            };
            wallDurability[2] = new UpgradeType("wallDurability3") {
                displayName = "Wall Durability - Tier III",
                tier = 3,
                minTier = 3,
                maxTier = 3,

                flags = new Flag[] { FlagTypes.wall },
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.thorium, 6),
                researchCost = ItemStack.With(Items.thorium, 120),

                previousUpgrade = wallDurability[1],

                properties = new BlockUpgradeMultipliers() {
                    entity_health = 0.2f,
                }
            };
            wallDurability[3] = new UpgradeType("wallDurability4") {
                displayName = "Wall Durability - Tier IV",
                tier = 4,
                minTier = 4,
                maxTier = 4,

                flags = new Flag[] { FlagTypes.wall },
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.copper, 6),
                researchCost = ItemStack.With(Items.copper, 120),

                previousUpgrade = wallDurability[2],

                properties = new BlockUpgradeMultipliers() {
                    entity_health = 0.2f,
                }
            };

            // Higher health, more weight, only works on fighters
            heavyFighterArmor = new UpgradeType[3];
            heavyFighterArmor[0] = new UpgradeType("heavyFighterArmor1") {
                displayName = "Heavy Fighter Armor - Tier I",
                tier = 1,

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.fighter },
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = heavyArmor[0],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.2f,
                    unit_emptyMass = 0.05f,
                    unit_maxVelocity = -0.075f,
                }
            };
            heavyFighterArmor[1] = new UpgradeType("heavyFighterArmor2") {
                displayName = "Heavy Fighter Armor - Tier II",
                tier = 2,

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.fighter },
                incopmatibleFlags = new Flag[] { FlagTypes.heavyArmored },

                installCost = ItemStack.With(Items.copper, 10, Items.silicon, 5),
                researchCost = ItemStack.With(Items.copper, 125, Items.silicon, 45),

                previousUpgrade = heavyFighterArmor[0],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.25f,
                    unit_emptyMass = 0.1f,
                    unit_maxVelocity = -0.1f,
                }
            };
            heavyFighterArmor[2] = new UpgradeType("heavyFighterArmor3") {
                displayName = "Heavy Fighter Armor - Tier III",
                tier = 3,

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.fighter },
                incopmatibleFlags = new Flag[] { FlagTypes.heavy, FlagTypes.heavyArmored },

                installCost = ItemStack.With(Items.silicon, 12, Items.titanium, 5),
                researchCost = ItemStack.With(Items.silicon, 160, Items.titanium, 35),

                previousUpgrade = heavyFighterArmor[1],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = 0.3f,
                    unit_emptyMass = 0.15f,
                    unit_maxVelocity = -0.125f,
                }
            };

            // Lower health, less weight, more velocity, (secondary) less drag, (secondary) more rotation speed
            lightArmor = new UpgradeType[5];
            lightArmor[0] = new UpgradeType("lightArmor1") {
                displayName = "Light Armor - Tier I",
                tier = 1,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = null,

                properties = new UnitUpgradeMultipliers() {
                    entity_health = -0.075f,
                    unit_emptyMass = -0.075f,
                    unit_maxVelocity = 0.05f,
                }
            };
            lightArmor[1] = new UpgradeType("lightArmor2") {
                displayName = "Light Armor - Tier II",
                tier = 2,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = lightArmor[0],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = -0.15f,
                    unit_emptyMass = -0.175f,
                    unit_maxVelocity = 0.1f,
                }
            };
            lightArmor[2] = new UpgradeType("lightArmor3") {
                displayName = "Light Armor - Tier III",
                tier = 3,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = lightArmor[1],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = -0.225f,
                    unit_emptyMass = -0.2f,
                    unit_maxVelocity = 0.15f,
                    flying_drag = -0.1f,
                }
            };
            lightArmor[3] = new UpgradeType("lightArmor4") {
                displayName = "Light Armor - Tier IV",
                tier = 4,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[] { FlagTypes.lightArmored },

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = lightArmor[2],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = -0.3f,
                    unit_emptyMass = -0.275f,
                    unit_maxVelocity = 0.2f,
                    flying_drag = -0.1f,
                    unit_rotationSpeed = 0.15f,                
                }
            };
            lightArmor[4] = new UpgradeType("lightArmor5") {
                displayName = "Light Armor - Tier V",
                tier = 5,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[] { FlagTypes.lightArmored, FlagTypes.light },

                installCost = ItemStack.With(Items.silicon, 5),
                researchCost = ItemStack.With(Items.silicon, 50),

                previousUpgrade = lightArmor[3],

                properties = new UnitUpgradeMultipliers() {
                    entity_health = -0.4f,
                    unit_emptyMass = -0.35f,
                    unit_maxVelocity = 0.375f,
                    flying_drag = -0.15f,
                    unit_rotationSpeed = 0.25f,
                }
            };

            // Higher fuel capacity, (secondary) less density
            fuelCapacity = new UpgradeType[3];
            fuelCapacity[0] = new UpgradeType("fuelCapacity1") {
                displayName = "Fuel Capacity - Tier I",
                tier = 1,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = null,

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelCapacity = 0.1f,
                }
            };
            fuelCapacity[1] = new UpgradeType("fuelCapacity2") {
                displayName = "Fuel Capacity - Tier II",
                tier = 2,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = fuelCapacity[0],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelCapacity = 0.2f,
                    unit_fuelDensity = -0.05f,
                }
            };
            fuelCapacity[2] = new UpgradeType("fuelCapacity3") {
                displayName = "Fuel Capacity - Tier III",
                tier = 3,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[] { FlagTypes.light },

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = fuelCapacity[1],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelCapacity = 0.35f,
                    unit_fuelDensity = -0.075f,
                }
            };

            // Lower fuel consume, (secondary) less capacity 
            fuelEfficiency = new UpgradeType[2];
            fuelEfficiency[0] = new UpgradeType("fuelEfficiency1") {
                displayName = "Fuel Efficiency - Tier I",
                tier = 1,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = fuelCapacity[0],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelConsumption = -0.075f,
                    unit_fuelCapacity = -0.05f,
                    unit_fuelDensity = 0.05f,
                }
            };
            fuelEfficiency[1] = new UpgradeType("fuelEfficiency2") {
                displayName = "Fuel Efficiency - Tier II",
                tier = 2,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 15, Items.metaglass, 5),
                researchCost = ItemStack.With(Items.silicon, 50, Items.metaglass, 50),

                previousUpgrade = fuelEfficiency[0],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelConsumption = -0.15f,
                    unit_fuelCapacity = -0.125f,
                    unit_fuelDensity = 0.1f,
                }
            };

            // Lower density, (secondary) more capacity
            fuelDensity = new UpgradeType[2];
            fuelDensity[0] = new UpgradeType("fuelDensity1") {
                displayName = "Fuel Density - Tier I",
                tier = 1,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = fuelCapacity[0],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelDensity = -0.1f,
                }
            };
            fuelDensity[1] = new UpgradeType("fuelDensity2") {
                displayName = "Fuel Density - Tier II",
                tier = 2,

                flags = new Flag[0],
                incopmatibleFlags = new Flag[0],

                installCost = ItemStack.With(Items.lead, 10),
                researchCost = ItemStack.With(Items.copper, 90, Items.metaglass, 30),

                previousUpgrade = fuelDensity[0],

                properties = new UnitUpgradeMultipliers() {
                    unit_fuelCapacity = 0.05f,
                    unit_fuelDensity = -0.2f,
                }
            };
        }
    }

    public class UpgradeType : Content {
        public string displayName;

        public int tier = 1;
        public bool isUnlocked;

        public int minTier = -1, maxTier = -1;
        public Flag[] incopmatibleFlags;

        public ItemStack[] installCost;
        public ItemStack[] researchCost;

        public UpgradeType previousUpgrade;

        public UpgradeMultipliers properties;

        public UpgradeType(string name) : base(name) {
            UpgradeHandler.Handle(this);  
        }

        public virtual bool IsValid() {
            // If requires an invalid flag, this upgrade can't be used and needs to be changed
            return !flags.HasAny(incopmatibleFlags);
        }

        public virtual bool CanBeResearched() {
            if (previousUpgrade == null) return true;
            return UpgradeResearcher.IsResearched(previousUpgrade);
        }

        public virtual bool CompatibleWith(EntityType entityType) {
            bool tierPass = (minTier == -1 || entityType.tier >= minTier) && (maxTier == -1 || entityType.tier <= maxTier);

            // Check if has all required flags and if has any incompatible flag
            bool hasFlags = entityType.flags.HasAll(flags);
            bool hasIncompatibleFlags = entityType.flags.HasAny(incopmatibleFlags);

            return tierPass && hasFlags && !hasIncompatibleFlags;
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