// Project:   Reanimate Mod for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date:  Feb 2022

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
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Player;

namespace ThePenwickPapers
{
    public class Reanimate : BaseEntityEffect
    {
        private static bool alreadyCastingReanimate; //used to prevent multiple reanimates in same spell bundle

        private string effectKey = "Reanimate";
        private DaggerfallLoot chosenVessel;
        private int soulChanceMod = 0;


        /// <summary>
        /// The soul value is used to determine the type of reanimated entity, its starting health,
        /// and the caster's ability to control it.
        /// </summary>
        public static int GetSoulValue(MobileTypes soulType)
        {
            return (int)soulType < soulStrength.Length ? soulStrength[(int)soulType] : 50;
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
            properties.ChanceCosts = MakeEffectCosts(12, 60, 220);
        }


        public override string GroupName => Text.ReanimateGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();



        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
            {
                return;
            }

            //to prevent casting multiple reanimates in same spell bundle
            if (alreadyCastingReanimate)
            {
                RefundSpellCost(manager);
                return;
            }

            bool success = false;
            
            try
            {
                if (HasRequiredItems())
                {
                    if (TryGetCorpse())
                    {
                        ShowSoulPicker();
                        success = true;
                        alreadyCastingReanimate = true;
                    }
                    else
                    {
                        Utility.AddHUDText(Text.NoViableVesselNearby.Get());
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


        public override void End()
        {
            base.End();
            alreadyCastingReanimate = false;
        }


        public override bool RollChance()
        {
            //modify the RollChance to include willpower bonus
            int modifiedChance = ChanceValue();
            modifiedChance += Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower);
            modifiedChance -= soulChanceMod; //more powerful souls are harder to control

            modifiedChance = Mathf.Clamp(modifiedChance, 3, 97);

            bool outcome = Dice100.SuccessRoll(modifiedChance);

            return outcome;
        }


        /// <summary>
        /// Checks the caster's inventory for the spell requirements: a ceremonial dagger and the soul
        /// of some poor sap.
        /// </summary>
        private bool HasRequiredItems()
        {
            ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate((int)ReligiousItems.Holy_dagger);
            DaggerfallUnityItem item = Caster.Entity.Items.GetItem(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_dagger, false, false, false);
            if (item == null)
            {
                //Whelp, the player left their sacrificial dagger in the glovebox again...
                string msg = Text.MissingHolyDagger.Get(itemTemplate.name);
                Utility.AddHUDText(msg);
                return false;
            }


            if (GetFilledSoulTraps().Count == 0)
            {
                itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate((int)MiscItems.Soul_trap);
                string msg = Text.MissingSoulTrap.Get(itemTemplate.name);
                Utility.AddHUDText(msg);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Who went shopping for souls today?
        /// </summary>
        private void ShowSoulPicker()
        {
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;

            List<MobileTypes> souls = GetFilledSoulTraps();
            if (souls.Count == 1)
            {
                //just one soul, selection window not needed
                StartSummoning(0);
                return;
            }

            DaggerfallListPickerWindow soulPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            soulPicker.OnItemPicked += SoulPicker_OnItemPicked;


            foreach (MobileTypes soul in souls)
            {
                string soulName = TextManager.Instance.GetLocalizedEnemyName((int)soul);
                soulPicker.ListBox.AddItem(soulName);
            }

            uiManager.PushWindow(soulPicker);
        }


        /// <summary>
        /// I'll take the frosted one, with sprinkles
        /// </summary>
        private void SoulPicker_OnItemPicked(int index, string soulName)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            StartSummoning(index);
        }


        /// <summary>
        /// Starts the reanimation process and handles costs.
        /// </summary>
        private void StartSummoning(int soulIndex)
        {
            List<MobileTypes> souls = GetFilledSoulTraps();

            GameObject go = Summon(souls[soulIndex]);

            //performing the reanimation ritual
            Transform camera = GameManager.Instance.MainCamera.transform;
            Vector3 position = camera.position + camera.forward;
            go.GetComponent<EnemyBlood>().ShowBloodSplash(0, position);

            DaggerfallUI.Instance.PlayOneShot(SoundClips.EquipShortBlade);

            Caster.Entity.DecreaseHealth(1);
            Caster.Entity.DecreaseFatigue(10, true);
            RemoveSoul(souls[soulIndex]);

            AdjustFactionReps(); //those Divines are loving you now
        }


        /// <summary>
        /// Checks the PC's inventory and returns the list of doomed souls available.
        /// </summary>
        private List<MobileTypes> GetFilledSoulTraps()
        {
            List<MobileTypes> filledTraps = new List<MobileTypes>();

            // Count regular filled soul gems
            ItemCollection casterItems = Caster.Entity.Items;
            for (int i = 0; i < casterItems.Count; i++)
            {
                DaggerfallUnityItem item = casterItems.GetItem(i);
                if (item != null && item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Soul_trap))
                {
                    if (item.TrappedSoulType != MobileTypes.None)
                    {
                        filledTraps.Add(item.TrappedSoulType);
                    }
                }
            }

            // Check for filled Azura's Star soul trap
            List<DaggerfallUnityItem> amulets = Caster.Entity.Items.SearchItems(ItemGroups.Jewellery, (int)Jewellery.Amulet);
            foreach (DaggerfallUnityItem amulet in amulets)
            {
                if (amulet.ContainsEnchantment(EnchantmentTypes.SpecialArtifactEffect, (short)ArtifactsSubTypes.Azuras_Star) && amulet.TrappedSoulType != MobileTypes.None)
                {
                    filledTraps.Add(amulet.TrappedSoulType);
                }
            }

            return filledTraps;
        }


        /// <summary>
        /// Remove the spent soul gem from the PC inventory (or clean out Azura's Star)
        /// </summary>
        void RemoveSoul(MobileTypes soulType)
        {
            // Remove regular filled soul traps matching soul type first
            ItemCollection casterItems = Caster.Entity.Items;
            for (int i = 0; i < casterItems.Count; i++)
            {
                DaggerfallUnityItem item = casterItems.GetItem(i);
                if (item != null && item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Soul_trap))
                {
                    if (item.TrappedSoulType == soulType)
                    {
                        //remove the soul, not the soul gem
                        item.TrappedSoulType = MobileTypes.None;
                        return;
                    }
                }
            }
            
            // Empty Azura's Star matching trapped soul type
            List<DaggerfallUnityItem> amulets = Caster.Entity.Items.SearchItems(ItemGroups.Jewellery, (int)Jewellery.Amulet);
            foreach (DaggerfallUnityItem amulet in amulets)
            {
                if (amulet.ContainsEnchantment(EnchantmentTypes.SpecialArtifactEffect, (short)ArtifactsSubTypes.Azuras_Star))
                {
                    if (amulet.TrappedSoulType == soulType)
                    {
                        amulet.TrappedSoulType = MobileTypes.None;
                        return;
                    }
                }
            }
        }


        /// <summary>
        /// Adjusts the player's reputation with various factions
        /// </summary>
        private void AdjustFactionReps()
        {
            if (Caster.EntityType != EntityTypes.Player)
            {
                return;
            }

            AdjustFactionRep((int)FactionFile.FactionIDs.Generic_Temple, -2);

            AdjustFactionRep((int)Temple.Divines.Akatosh, -3);
            AdjustFactionRep((int)Temple.Divines.Arkay, -9);
            AdjustFactionRep((int)Temple.Divines.Dibella, -3);
            AdjustFactionRep((int)Temple.Divines.Julianos, -1);
            AdjustFactionRep((int)Temple.Divines.Kynareth, -3);
            AdjustFactionRep((int)Temple.Divines.Mara, -4);
            AdjustFactionRep((int)Temple.Divines.Stendarr, -4);
            AdjustFactionRep((int)Temple.Divines.Zenithar, -3);

            AdjustFactionRep((int)FactionFile.FactionIDs.Meridia, -6);

            //boost rep with a few factions
            AdjustFactionRep((int)FactionFile.FactionIDs.King_of_Worms, 3);
            AdjustFactionRep((int)FactionFile.FactionIDs.Molag_Bal, 3);

        }


        /// <summary>
        /// Adjusts the player's reputation with the specified faction
        /// </summary>
        private void AdjustFactionRep(int factionID, int amount)
        {
            PersistentFactionData factions = GameManager.Instance.PlayerEntity.FactionData;
            FactionFile.FactionData factionData = factions.FactionDict[factionID];

            //change rep for this faction as well as all child factions
            factions.PropagateReputationChange(factionData, factionID, amount);
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


        /// <summary>
        /// Examines the location the player is looking at and checks if there is an appropriate
        /// human-like vessel available in range.
        /// </summary>
        private bool TryGetCorpse()
        {
            List<DaggerfallLoot> corpses = new List<DaggerfallLoot>();

            DaggerfallLoot[] lootMarkers = UnityEngine.Object.FindObjectsOfType<DaggerfallLoot>();
            foreach (DaggerfallLoot loot in lootMarkers)
            {
                if (Vector3.Distance(Caster.transform.position, loot.transform.position) < 3)
                {
                    if (CanReanimate(loot))
                    {
                        corpses.Add(loot);
                    }
                }
            }

            foreach (DaggerfallLoot corpse in corpses)
            {
                Vector3 casterXZ = Vector3.ProjectOnPlane(caster.transform.position, Vector3.up);
                Vector3 targetXZ = Vector3.ProjectOnPlane(corpse.transform.position, Vector3.up);
                Vector3 direction = targetXZ - casterXZ;
                if (Vector3.Angle(Caster.transform.forward, direction) < 25)
                {
                    chosenVessel = corpse;
                    return true;
                }
            }

            chosenVessel = null;
            return false;
        }


        private static readonly MobileTypes[] ViableMonsters =
        {
            MobileTypes.Knight_CityWatch, MobileTypes.Lich, MobileTypes.AncientLich, MobileTypes.Mummy,
            MobileTypes.SkeletalWarrior, MobileTypes.Vampire, MobileTypes.VampireAncient, MobileTypes.Zombie
        };

        /// <summary>
        /// Determines if the specied corpse can be reanimated.
        /// Possible corpses include the human enemy classes as well as skeletons, vampires, zombies, lichs,
        /// mummies, and city watchmen.
        /// </summary>
        private bool CanReanimate(DaggerfallLoot corpse)
        {
            if (corpse.isEnemyClass)
            {
                return true;
            }
            else if (corpse.entityName != null)
            {
                foreach (MobileTypes monsterType in ViableMonsters)
                {
                    if (corpse.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)monsterType)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Begins the summoning animation and creates an undead creature corresponding to the selected corpse.
        /// </summary>
        private GameObject Summon(MobileTypes soulType)
        {
            string displayName = string.Format("Penwick Summoned[{0}]", chosenVessel.entityName);

            Transform parent = GameObjectHelper.GetBestParent();

            Vector3 location = chosenVessel.transform.position;

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);

            go.SetActive(false);

            MobileTypes minionType;

            if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.SkeletalWarrior))
                || chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Lich))
                || chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.AncientLich))
                )
            {
                if (soulType == MobileTypes.Lich)
                    minionType = MobileTypes.Lich;
                else if (soulType == MobileTypes.AncientLich)
                    minionType = MobileTypes.AncientLich;
                else
                    minionType = MobileTypes.SkeletalWarrior;
            }
            else if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Mummy)))
            {
                minionType = MobileTypes.Mummy;
            }
            else
            {
                minionType = MobileTypes.Zombie;
            }

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            // Configure summons
            soulChanceMod = GetSoulValue(soulType); //stronger souls are harder to control
            bool allied = Caster.EntityType == EntityTypes.Player && RollChance();

            setupEnemy.ApplyEnemySettings(minionType, MobileReactions.Hostile, MobileGender.Unspecified, 0, allied);
            setupEnemy.AlignToGround();

            //additional magnitude-related adjustments
            AdjustUndeadMinion(go, soulType);

            DaggerfallEnemy minion = go.GetComponent<DaggerfallEnemy>();

            //needs a loadID to save/serialize
            minion.LoadID = DaggerfallUnity.NextUID;

            //Have the reanimated entity looking in same direction as caster
            minion.transform.rotation = Caster.transform.rotation;

            //Move any inventory items from the corpse to the reanimated entity and destroy the corpse
            ItemCollection creatureInventory = go.GetComponent<DaggerfallEntityBehaviour>().Entity.Items;
            creatureInventory.Clear();
            creatureInventory.TransferAll(chosenVessel.Items);
            GameObject.Destroy(chosenVessel.gameObject);

            //to allow interaction with the reanimated corpse, assuming it's an ally
            PenwickMinion.AddNewMinion(go.GetComponent<DaggerfallEntityBehaviour>());

            Texture2D eggTexture = ThePenwickPapersMod.Instance.SummoningEggTexture;
            AudioClip sound = ThePenwickPapersMod.Instance.ReanimateWarp;
            SummoningEgg egg = new SummoningEgg(minion, eggTexture, new Color32(200, 0, 0, 255), sound);

            //start coroutine to animate the 'hatching' process
            IEnumerator coroutine = egg.Hatch();
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);

            return go;
        }


        /// <summary>
        /// Sets the health for the new undead minion.
        /// </summary>
        private void AdjustUndeadMinion(GameObject minion, MobileTypes soulType)
        {

            MobileUnit mobileUnit = minion.GetComponentInChildren<MobileUnit>();

            //Other undead in the game have random health within a certain range.
            //We want ours tied to the soul type used to reanimate
            MobileEnemy mobileEnemy = mobileUnit.Enemy;             //struct copy

            if (mobileEnemy.ID == (int)MobileTypes.Zombie)
            {
                //In vanilla Daggerfall, zombies have a narrow health range (52-66).
                mobileEnemy.MinHealth = 52 + GetSoulValue(soulType) / 4;
            }
            else if (mobileEnemy.ID == (int)MobileTypes.SkeletalWarrior || mobileEnemy.ID == (int)MobileTypes.Mummy)
            {
                //In vanilla Daggerfall, skeletons and mummies have a wide health range (17-66).
                mobileEnemy.MinHealth = 17 + GetSoulValue(soulType);
            }
            else if (mobileEnemy.ID == (int)MobileTypes.Lich || mobileEnemy.ID == (int)MobileTypes.AncientLich)
            {
                //In vanilla Daggerfall, liches have a wide health range (30-170).
                mobileEnemy.MinHealth = 40 + GetSoulValue(soulType);
            }
            else
            {
                mobileEnemy.MinHealth += GetSoulValue(soulType);
            }

            int luckMod = Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Luck) / 12;

            mobileEnemy.MaxHealth = mobileEnemy.MinHealth + luckMod;

            if (Caster.EntityType != EntityTypes.Player && ChanceSuccess)
            {
                mobileEnemy.Team = Caster.Entity.Team;
            }

            if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Knight_CityWatch)))
            {
                //with every fiber of their being...
                EnemySounds sounds = minion.GetComponent<EnemySounds>();
                sounds.BarkSound = SoundClips.Halt;
            }

            //Record MobileEnemy changes to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Hostile, 0);

            DaggerfallEntityBehaviour behaviour = minion.GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            entity.SetEnemyCareer(mobileEnemy, behaviour.EntityType);
        }


        private TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ReanimateEffectDescription1.Get(),
                Text.ReanimateEffectDescription2.Get(),
                Text.ReanimateDuration.Get(),
                Text.ReanimateSpellMakerChance.Get(),
                Text.ReanimateMagnitude.Get());
        }

        private TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ReanimateDuration.Get(),
                Text.ReanimateSpellBookChance1.Get(),
                Text.ReanimateSpellBookChance2.Get(),
                Text.ReanimateMagnitude.Get(),
                "",
                "\"" + Text.ReanimateEffectDescription1.Get(),
                Text.ReanimateEffectDescription2.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("mysticism") + "]");
        }



        //modifed version classicParamsCosts table found in MagicAndEffects/Effects/Enchanting/SoulBound.cs
        // Matches monster IDs 0-42
        private static short[] soulStrength =
        {
            0,  //-0,      //Rat 
            2,  //-10,     //Imp 
            6,  //-20,     //Spriggan 
            0,  //-0,      //GiantBat
            0,  //-0,      //GrizzlyBear
            0,  //-0,      //SabertoothTiger
            0,  //-0,      //Spider
            3,  //-10,     //Orc
            11,  //-30,     //Centaur
            18,  //-90,    //Werewolf
            20,  //-100,   //Nymph
            0,  //-0,      //Slaughterfish
            5,  //-10,     //OrcSergeant
            8,  //-30,     //Harpy
            24,  //-140,   //Wereboar
            0,  //-0,      //SkeletalWarrior
            12,  //-30,     //Giant
            0,  //-0,      //Zombie
            40,  //-300,   //Ghost
            21,  //-100,   //Mummy
            0,  //-0,      //GiantScorpion
            10,  //-30,     //OrcShaman
            11,  //-30,     //Gargoyle
            40,  //-300,   //Wraith
            7,  //-10,     //OrcWarlord
            60,  //-500,   //FrostDaedra
            60,  //-500,   //FireDaedra
            22,  //-100,   //Daedroth
            63,  //-700,   //Vampire
            80,  //-1500,  //DaedraSeducer
            72,  //-1000,  //VampireAncient
            140,  //-8000, //DaedraLord
            72,  //-1000,  //Lich
            110,  //-2500, //AncientLich
            0,  //-0,      //Dragonling (no soul, general spawn)
            38,  //-300,   //FireAtronach
            38,  //-300,   //IronAtronach
            38,  //-300,   //FleshAtronach
            38,  //-300,   //IceAtronach
            0,  //-0,      //Horse_Invalid
            124,  //-5000, //Dragonling_Alternate (has soul, quest spawn only)
            20,  //-100,   //Dreugh
            20,  //-100,   //Lamia
        };


    } //class Reanimate


} //namespace
