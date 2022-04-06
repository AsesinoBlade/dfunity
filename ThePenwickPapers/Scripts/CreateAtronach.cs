// Project:   Create Atronach Mod for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date:  June 2021

using System;
using System.Collections;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;

namespace ThePenwickPapers
{
    public class CreateAtronach : BaseEntityEffect
    {
        private static readonly string effectKey = "Create-Atronach";

        // Variant can be stored internally with any format
        struct VariantProperties
        {
            public string subGroupKey;
            public EffectProperties effectProperties;
            public MobileTypes mobileType;
            public Color32 eggColor;
            public ItemGroups itemGroup;
            public int itemIndex;
        }

        private readonly VariantProperties[] variants = new VariantProperties[]
        {
            new VariantProperties()
            {
                subGroupKey = "Fire",
                mobileType = MobileTypes.FireAtronach,
                eggColor = new Color32(255, 140, 4, 255),
                itemGroup = ItemGroups.MetalIngredients,
                itemIndex = (int) MetalIngredients.Sulphur
            },
            new VariantProperties()
            {
                subGroupKey = "Flesh",
                mobileType = MobileTypes.FleshAtronach,
                eggColor = new Color32(80, 175, 40, 255),
                itemGroup = ItemGroups.MiscellaneousIngredients1,
                itemIndex = (int) MiscellaneousIngredients1.Elixir_vitae
            },
            new VariantProperties()
            {
                subGroupKey = "Ice",
                mobileType = MobileTypes.IceAtronach,
                eggColor = new Color32(50, 100, 255, 255),
                itemGroup = ItemGroups.MiscellaneousIngredients1,
                itemIndex = (int) MiscellaneousIngredients1.Pure_water
            },
            new VariantProperties()
            {
                subGroupKey = "Iron",
                mobileType = MobileTypes.IronAtronach,
                eggColor = new Color32(80, 80, 90, 255),
                itemGroup = ItemGroups.MetalIngredients,
                itemIndex = (int) MetalIngredients.Lodestone
            }
        };



        // Must override Properties to return correct properties for any variant
        // The currentVariant value is set by magic framework - each variant gets enumerated to its own effect template
        public override EffectProperties Properties
        {
            get { return variants[currentVariant].effectProperties; }
        }


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.ShowSpellIcon = false;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = ElementTypes.Magic;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Mysticism;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportChance = true;
            properties.ChanceFunction = ChanceFunction.Custom;
            properties.ChanceCosts = MakeEffectCosts(12, 60, 150);
            properties.SupportMagnitude = true;
            properties.MagnitudeCosts = MakeEffectCosts(16, 80, 190);

            // Set variant count so framework knows how many to extract
            variantCount = variants.Length;

            // Set properties unique to each variant
            for (int i = 0; i < variantCount; ++i)
            {
                variants[i].effectProperties = properties; //making a copy of default properties struct
                variants[i].effectProperties.Key = string.Format("{0}-{1}", effectKey, variants[i].subGroupKey);
            }
        }

        public override string GroupName => Text.CreateAtronachGroupName.Get();
        public override string SubGroupName => variants[currentVariant].subGroupKey;
        public override string DisplayName => Text.CreateAtronachDisplayName.Get(SubGroupName);
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
            {
                return;
            }

            bool success = false;
            
            try
            {
                if (variants[currentVariant].mobileType == MobileTypes.FireAtronach && GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                {
                    Utility.AddHUDText(Text.CantCreateFireAtronachSubmerged.Get());
                }
                else
                {
                    //requires spell component
                    DaggerfallUnityItem ingredient;

                    if (TryGetSpellComponent(out ingredient))
                    {
                        Vector3 location;
                        if (TryGetSpawnLocation(out location))
                        {
                            Summon(location);
                            Caster.Entity.Items.RemoveOne(ingredient);
                            success = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                Debug.LogException(e);
            }

            if (!success)
            {
                RefundSpellCost(manager);
                End();
            }
        }


        public override bool RollChance()
        {
            //modify the RollChance to include willpower bonus
            int modifiedChance = ChanceValue();
            modifiedChance += Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 4;

            modifiedChance = Mathf.Clamp(modifiedChance, 3, 97);

            bool outcome = Dice100.SuccessRoll(modifiedChance);

            return outcome;
        }


        /// <summary>
        /// Checks the caster's inventory for the spell ingredient/component
        /// </summary>
        /// <returns>true if the component was found, false otherwise</returns>
        private bool TryGetSpellComponent(out DaggerfallUnityItem item)
        {
            VariantProperties variant = variants[currentVariant];

            item = Caster.Entity.Items.GetItem(variant.itemGroup, variant.itemIndex, false, false, true);

            if (item == null)
            {
                ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(variant.itemIndex);
                string msg = Text.MissingComponent.Get(itemTemplate.name, variant.subGroupKey);
                Utility.AddHUDText(msg);
                return false;
            }

            return true;
        }



        /// <summary>
        /// Refund magicka cost of this effect to the caster
        /// </summary>
        private void RefundSpellCost(EntityEffectManager manager)
        {
            if (manager.ReadySpell != null)
            {
                foreach (EffectEntry entry in manager.ReadySpell.Settings.Effects)
                {
                    if (entry.Key.Equals(Key) && entry.Settings.Equals(Settings))
                    {
                        FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
                        Caster.Entity.IncreaseMagicka(cost.spellPointCost);
                        break;
                    }
                }
            }
        }

        private static readonly float[] scanDistances = { 2.0f, 3.0f, 1.2f };
        private static readonly float[] scanDownUpRots = { 45, 30, 0, -30, -45 };
        private static readonly float[] scanLeftRightRots = { 0, 5, -5, 15, -15, 30, -30, 45, -45 };

        /// <summary>
        /// Scans the area in front of the caster and tries to find a location that can fit a medium-sized creature.
        /// </summary>
        private bool TryGetSpawnLocation(out Vector3 location)
        {
            int casterLayerMask = ~(1 << Caster.gameObject.layer);

            //try to find reasonable spawn location in front of the caster
            foreach (float distance in scanDistances)
            {
                foreach (float downUpRot in scanDownUpRots)
                {
                    foreach (float leftRightRot in scanLeftRightRots)
                    {
                        Quaternion rotation = Quaternion.Euler(downUpRot, leftRightRot, 0);
                        Vector3 direction = (Caster.transform.rotation * rotation) * Vector3.forward;

                        //shouldn't be anything between the caster and spawn point
                        Ray ray = new Ray(Caster.transform.position, direction);
                        RaycastHit hit; //might be useful for debugging
                        if (Physics.Raycast(ray, out hit, distance, casterLayerMask))
                        {
                            continue;
                        }

                        //create a reasonably sized capsule to check if enough space is available for spawning
                        Vector3 scannerPos = Caster.transform.position + (direction * distance);
                        Vector3 top = scannerPos + Vector3.up * 0.4f;
                        Vector3 bottom = scannerPos - Vector3.up * 0.4f;
                        float radius = 0.4f; //radius*2 included in height
                        if (!Physics.CheckCapsule(top, bottom, radius))
                        {
                            //just returning first available valid position
                            location = scannerPos;
                            return true;
                        }
                    }
                }
            }

            location = Vector3.zero;
            return false;
        }



        /// <summary>
        /// Creates the atronach and begins the summoning animation.
        /// </summary>
        private void Summon(Vector3 location)
        {
            VariantProperties variant = variants[currentVariant];

            string displayName = string.Format("Penwick Summoned[{0}]", variant.mobileType.ToString());

            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);

            go.SetActive(false);

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            // Configure summons
            bool allied = ChanceSuccess && Caster.EntityType == EntityTypes.Player;

            setupEnemy.ApplyEnemySettings(variant.mobileType, MobileReactions.Hostile, MobileGender.Unspecified, 0, allied);
            setupEnemy.AlignToGround();

            //additional magnitude-related adjustments
            AdjustAtronach(go);

            DaggerfallEnemy creature = go.GetComponent<DaggerfallEnemy>();

            //needs a loadID to save/serialize
            creature.LoadID = DaggerfallUnity.NextUID;

            GameManager.Instance.RaiseOnEnemySpawnEvent(go);

            //Have atronach looking in same direction as caster
            creature.transform.rotation = Caster.transform.rotation;

            //to allow interaction with the summoned creature
            PenwickMinion.AddNewMinion(go.GetComponent<DaggerfallEntityBehaviour>());

            Texture2D eggTexture = ThePenwickPapersMod.Instance.SummoningEggTexture;
            AudioClip sound = ThePenwickPapersMod.Instance.WarpIn;
            SummoningEgg egg = new SummoningEgg(creature, eggTexture, variant.eggColor, sound);

            //start coroutine to animate the 'hatching' process
            IEnumerator coroutine = egg.Hatch();
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Adjusts the atronach's health.
        /// </summary>
        private void AdjustAtronach(GameObject atronach)
        {
            MobileUnit mobileUnit = atronach.GetComponentInChildren<MobileUnit>();

            //other atronachs in the game have random health with a large range
            //we want ours tied to spell magnitude
            int luckBonus = Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Luck) / 8;
            MobileEnemy mobileEnemy = mobileUnit.Enemy;
            int range = mobileEnemy.MaxHealth - mobileEnemy.MinHealth;
            int magnitude = Mathf.Clamp(GetMagnitude(caster), 1, range);
            mobileEnemy.MinHealth += magnitude - 2;
            mobileEnemy.MaxHealth = mobileEnemy.MinHealth + luckBonus;

            if (Caster.EntityType != EntityTypes.Player && ChanceSuccess)
            {
                mobileEnemy.Team = Caster.Entity.Team;
            }

            //Record MobileEnemy changes to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Hostile, 0);

            DaggerfallEntityBehaviour behaviour = atronach.GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            entity.SetEnemyCareer(mobileEnemy, behaviour.EntityType);
        }


        private TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                GetEffectDescription(),
                Text.CreateAtronachDuration.Get(),
                Text.CreateAtronachSpellMakerChance.Get(),
                Text.CreateAtronachSpellMakerMagnitude.Get());
        }

        private TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.CreateAtronachDuration.Get(),
                Text.CreateAtronachSpellBookChance1.Get(),
                Text.CreateAtronachSpellBookChance2.Get(),
                Text.CreateAtronachSpellBookMagnitude.Get(),
                "",
                "\"" + GetEffectDescription() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("mysticism") + "]");
        }


        private string GetEffectDescription()
        {
            ItemTemplate item = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(variants[currentVariant].itemIndex);
            return Text.CreateAtronachEffectDescription.Get(SubGroupName, item.name);
        }

    }
}
