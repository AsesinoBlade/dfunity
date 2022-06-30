// Project:     Herbalism, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using DaggerfallConnect;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace ThePenwickPapers
{

    public class Herbalism
    {
        public class Remedy
        {
            public Text Name;
            public int RequiredSkill;
            public DFCareer.Stats Stat;
            public Text TreatmentDescription;
            public SoundClips[] Sounds;
            public int[] Ingredients;

            public Remedy(Text remedyName, int requiredSkill, DFCareer.Stats stat, Text description, SoundClips[] sounds, int[] ingredients)
            {
                Name = remedyName;
                RequiredSkill = requiredSkill;
                Stat = stat;
                TreatmentDescription = description;
                Sounds = sounds;
                Ingredients = ingredients;
            }
        }

        public static readonly List<Remedy> Remedies = new List<Remedy>()
        {
            new Remedy(Text.RecoverFatigue, 16, DFCareer.Stats.None, Text.TreatLethargyViaExhilarantTonic,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Bamboo, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverFatigue, 30, DFCareer.Stats.None, Text.TreatLethargyViaErgogenicInfusion,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Ginkgo_leaves, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RegenerateHealth, 17, DFCareer.Stats.None, Text.TreatSalubriousAccelerantViaBotanicalTincture,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.AmbientWaterBubbles},
                new int[] { (int)PlantIngredients1.Root_bulb, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RegenerateHealth, 33, DFCareer.Stats.None, Text.TreatSalubriousAccelerantViaRemedialPoultice,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients1.Troll_blood, (int)PlantIngredients2.Aloe}),

            new Remedy(Text.RecoverMagicka, 18, DFCareer.Stats.None, Text.TreatFluxePaucityViaMetallurgicCordial,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)MetalIngredients.Silver, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverMagicka, 35, DFCareer.Stats.None, Text.TreatFluxePaucityViaDraconicIncense,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.Burning},
                new int[] {(int)CreatureIngredients2.Dragons_scales, (int)Gems.Amber}),

            new Remedy(Text.ExpungePoison, 19, DFCareer.Stats.None, Text.TreatToxaemiaViaRenalExpungement,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)MetalIngredients.Copper, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.CleansePoison, 29, DFCareer.Stats.None, Text.TreatToxaemiaViaDiaphoreticDepurative,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Clover, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RecoverStrength, 24, DFCareer.Stats.Strength, Text.TreatAtrophiaViaAnapleroticTincture,
                new SoundClips[] {SoundClips.AmbientWaterBubbles, SoundClips.MakePotion},
                new int[] {(int)CreatureIngredients1.Orcs_blood, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RecoverStrength, 36, DFCareer.Stats.Strength, Text.TreatAtrophiaViaAnapleroticUnction,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients3.Wereboar_tusk, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.RecoverIntelligence, 22, DFCareer.Stats.Intelligence, Text.TreatPhrenitisViaAntiphlogisticTonic,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.White_rose, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverIntelligence, 37, DFCareer.Stats.Intelligence, Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)CreatureIngredients1.Lich_dust, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverWillpower, 21, DFCareer.Stats.Willpower, Text.TreatDeliriumViaBotanicalCordial,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] { (int)PlantIngredients1.Green_berries, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverWillpower, 36, DFCareer.Stats.Willpower, Text.TreatAmentiaViaBotanicalEnema,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Green_berries, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverAgility, 23, DFCareer.Stats.Agility, Text.TreatAtaxiaViaAntispasmodicSalve,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)PlantIngredients1.Green_leaves, (int)PlantIngredients2.Aloe}),

            new Remedy(Text.RecoverAgility, 38, DFCareer.Stats.Agility,Text.TreatAtaxiaViaAntispasmodicEnema,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Green_leaves, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverEndurance, 25, DFCareer.Stats.Endurance, Text.TreatAnaemiaViaHepaticDepurative,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Bamboo, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.RecoverEndurance, 39, DFCareer.Stats.Endurance, Text.TreatHepaticPhlegmasiaViaFloralIncense,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.Burning},
                new int[] {(int)PlantIngredients1.Yellow_rose, (int)Gems.Amber}),

            new Remedy(Text.RecoverPersonality, 28, DFCareer.Stats.Personality, Text.TreatDistemperViaSoothingCordial,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Golden_poppy, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverPersonality, 40, DFCareer.Stats.Personality, Text.TreatEffluviaViaRectifyingDecoction,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Palm, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverSpeed, 27, DFCareer.Stats.Speed, Text.TreatKinesiaViaCalomel,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Yellow_berries, (int)MetalIngredients.Mercury}),

            new Remedy(Text.ResistParalysis, 20, DFCareer.Stats.None, Text.TreatCatylepsyViaAntiparalyticOintment,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients1.Basilisk_eye, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.Moonseed, 26, DFCareer.Stats.None, Text.Envenomed,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Red_berries, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.Magebane, 31, DFCareer.Stats.None, Text.Envenomed,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients2.Black_poppy, (int)CreatureIngredients1.Wraith_essence}),

            new Remedy(Text.PyrrhicAcid, 44, DFCareer.Stats.None, Text.Envenomed,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients2.Black_rose, (int)PlantIngredients2.Cactus}),

        };

        private static DaggerfallEntityBehaviour patient;
        private static DaggerfallListPickerWindow remedyPicker;
        private static bool creatingRemedy;



        /// <summary>
        /// Checks activate details and character info to see if conditions are met to trigger
        /// herbalism remedies selection window.
        /// Shows remedy selection screen if so.
        /// </summary>
        public static bool AttemptRemedy(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;

            bool hitTerrain = hitInfo.collider is TerrainCollider || hitInfo.collider is MeshCollider;

            bool hitFriendly = false;
            DaggerfallEntityBehaviour creature = hitInfo.collider.GetComponent<DaggerfallEntityBehaviour>();
            if (creature && (creature.EntityType == EntityTypes.EnemyClass || creature.EntityType == EntityTypes.EnemyMonster))
            {
                EnemyMotor motor = creature.GetComponent<EnemyMotor>();
                EnemyEntity entity = creature.Entity as EnemyEntity;
                MobileEnemy mobileEnemy = entity.MobileEnemy;
                if (mobileEnemy.Affinity == MobileAffinity.Undead || mobileEnemy.Affinity == MobileAffinity.Golem)
                    return false; //can't treat these types

                hitFriendly = entity.MobileEnemy.Team == MobileTeams.PlayerAlly || !motor.IsHostile;
            }

            if (hitInfo.distance > 2.5)
                return false;
            else if (!hitTerrain && !hitFriendly)
                return false;
            else if (!GameManager.Instance.PlayerMotor.IsCrouching)
                return false;
            else if (hitTerrain && Vector3.Angle(hitInfo.normal, Vector3.up) > 25)
                return false; //if treating self, must activate floor or table

            if (creatingRemedy)
                return true; //already working on something

            if (Utility.IsPlayerThreatened())
            {
                Utility.AddHUDText(Text.NowIsNotTheTime.Get());
                return true;
            }

            if (GameManager.Instance.PlayerEntity.IsParalyzed)
            {
                Utility.AddHUDText(Text.YouAreParalyzed.Get());
                return true;
            }

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
            {
                Utility.AddHUDText(Text.NotWhileSubmerged.Get());
                return true;
            }

            bool mortarEquipped = false;
            foreach (DaggerfallUnityItem item in GameManager.Instance.PlayerEntity.ItemEquipTable.EquipTable)
            {
                if (item != null && item.TemplateIndex == MortarAndPestle.MortarAndPestleTemplateIndex)
                    mortarEquipped = true;
            }

            if (!mortarEquipped)
            {
                Utility.AddHUDText(Text.MustHaveMortarPestle.Get());
                return true;
            }

            if (hitFriendly)
                EnhancedInfo.ShowPatientInfo(creature);

            patient = hitFriendly ? creature : GameManager.Instance.PlayerEntityBehaviour;
            ShowRemedyPicker();

            return true;
        }


        /// <summary>
        /// Called when a game is loaded.
        /// </summary>
        public static void OnLoadEvent()
        {
            //make sure static state variables are properly set so we don't get stuck
            creatingRemedy = false;
        }


        /// <summary>
        /// Show color coded available remedy selections in list window.
        /// </summary>
        private static void ShowRemedyPicker()
        {
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;

            int medical = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            remedyPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            remedyPicker.OnItemPicked += RemedyPicker_OnItemPicked;
            remedyPicker.OnCancel += RemedyPicker_OnCancel;

            ListBox listBox = remedyPicker.ListBox;
            ListBox.ListItem item;
            int count = 0;
            listBox.SelectNone();

            foreach (Remedy remedy in Remedies)
            {
                if (remedy.RequiredSkill > medical)
                    continue;

                ++count;

                StringBuilder sb = new StringBuilder();
                sb.Append(remedy.Name.Get());
                sb.Append(" (");
                for (int i = 0; i < remedy.Ingredients.Length; ++i)
                {
                    if (i > 0)
                        sb.Append(", ");

                    ItemTemplate template = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(remedy.Ingredients[i]);
                    sb.Append(template.name);
                }
                sb.Append(")");

                listBox.AddItem(sb.ToString(), out item, -1, remedy);

                item.tag = remedy;

                if (!IsRemedyNeeded(remedy))
                {
                    item.textColor = new Color(0.4f, 0.8f, 0.4f); ;
                    item.highlightedTextColor = item.textColor;
                }

                if (!HasAllIngredients(remedy))
                {
                    item.textColor = new Color(0.8f, 0.8f, 0.8f);
                    item.highlightedTextColor = item.textColor;
                }
            }

            if (count == 0)
            {
                string skillName = DaggerfallUnity.Instance.TextProvider.GetSkillName(DFCareer.Skills.Medical);
                Utility.AddHUDText(Text.HerbalismUnknown.Get(skillName));
                return;
            }
            uiManager.PushWindow(remedyPicker);
        }


        /// <summary>
        /// Checks if PC has all ingredients necessary to create remedy.
        /// </summary>
        private static bool HasAllIngredients(Remedy remedy)
        {
            bool hasAll = true;

            foreach (int ingredient in remedy.Ingredients)
                hasAll &= HasItem(ingredient);

            return hasAll;
        }


        /// <summary>
        /// Checks if PC has specified item in their inventory.
        /// Ignore quest items.
        /// </summary>
        private static bool HasItem(int itemTemplateIndex)
        {
            ItemCollection items = GameManager.Instance.PlayerEntity.Items;

            for (int i = 0; i < items.Count; i++)
            {
                DaggerfallUnityItem item = items.GetItem(i);
                if (!item.IsQuestItem && item.IsOfTemplate(itemTemplateIndex))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Triggered when a remedy is selected from the list-picker window.
        /// Starts the remedy production coroutine.
        /// </summary>
        private static void RemedyPicker_OnItemPicked(int index, string itemText)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            //The remedy picker window prevents 'Action Complete' from being seen, so we must
            //manually stop swallowing actions
            ThePenwickPapersMod.StopSwallowingActions();

            ListBox.ListItem item = remedyPicker.ListBox.GetItem(index);
            Remedy remedy = (Remedy)item.tag;

            if (!HasAllIngredients(remedy))
            {
                Utility.AddHUDText(Text.RemedyIngredientsNotInInventory.Get());
                return;
            }

            if (IsRemedyNeeded(remedy))
            {
                IEnumerator coroutine = CreateRemedy(remedy);
                ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
            }
            else
            {
                Utility.AddHUDText(Text.NotNecessary.Get());
            }

        }

        /// <summary>
        /// Triggered when the Escape key is pressed to cancel the list picker window
        /// </summary>
        private static void RemedyPicker_OnCancel(DaggerfallPopupWindow sender)
        {
            //The remedy picker window prevents 'Action Complete' from being seen, so we must
            //manually stop swallowing actions
            ThePenwickPapersMod.StopSwallowingActions();
        }


        /// <summary>
        /// Coroutine produces sound effects when creating remedies, then applies the remedy when complete.
        /// </summary>
        private static IEnumerator CreateRemedy(Remedy remedy)
        {
            creatingRemedy = true;

            Camera camera = GameManager.Instance.MainCamera;

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;

            DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();

            foreach (SoundClips sound in remedy.Sounds)
            {
                dfAudioSource.PlayOneShot(sound, 1, 0.5f);

                AudioClip audioClip = dfAudioSource.GetAudioClip((int)sound);

                const float tick = 0.1f;

                float length = audioClip.length > 2f ? 2f : audioClip.length;

                for (float time = 0; time < length; time += tick)
                {
                    float distance = Vector3.Distance(camera.transform.position, originalPosition);
                    float angle = Quaternion.Angle(camera.transform.rotation, originalRotation);
                    if (distance > 0.3f || angle > 20)
                    {
                        Utility.AddHUDText(Text.HerbalismInterrupted.Get());
                        dfAudioSource.AudioSource.Stop();
                        creatingRemedy = false;
                        yield break;
                    }
                    yield return new WaitForSeconds(tick);
                }
            }

            ApplyRemedy(remedy);

            creatingRemedy = false;
        }


        /// <summary>
        /// Applies the appropriate HerbalRemedy incumbent effect status for the selected remedy.
        /// Removes the required remedy ingredients from PC inventory.
        /// </summary>
        private static void ApplyRemedy(Remedy remedy)
        {
            //Using herbalism causes any existing envenomed state to be removed.
            ExitEnvenomedState();

            switch (remedy.Name)
            {
                case Text.ExpungePoison:
                case Text.CleansePoison:
                    int roundsRemaining = TreatPoison(remedy.Name.Equals(Text.CleansePoison));
                    if (roundsRemaining < 1)
                        Utility.AddDelayedHUDText(Text.PoisonCompletelyNeutralized.Get(), 3.0f);
                    else if (roundsRemaining < 8)
                        Utility.AddDelayedHUDText(Text.PoisonPartiallyNeutralized.Get(), 3.0f);
                    else
                        Utility.AddDelayedHUDText(Text.PoisonNeutralizeMuchRemains.Get(), 3.0f);

                    ShowRemedyDescriptiveText(remedy);
                    break;
                case Text.Moonseed:
                case Text.Magebane:
                case Text.PyrrhicAcid:
                    EnterEnvenomedState(remedy);
                    Utility.AddHUDText(Text.Envenomed.Get());
                    break;
                default:
                    ApplyRemedyEffect(remedy);
                    ShowRemedyDescriptiveText(remedy);
                    break;
            }

            ApplySideEffects(remedy);

            RemoveIngredients(remedy);

            //medical skill has a large advancement multiplier (12)
            GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Medical, 20);
        }


        /// <summary>
        /// Shows appropriate flavor text related to the applied remedy as HUD text.
        /// </summary>
        private static void ShowRemedyDescriptiveText(Remedy remedy)
        {
            int intelligence = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Intelligence);
            if (intelligence > 50)
            {
                string prefix = Text.TreatPrefix.Get();
                string description = string.Format("\"{0} {1}\"", prefix, remedy.TreatmentDescription.Get());
                Utility.AddHUDText(description);
            }
            else if (intelligence < 35)
            {
                Utility.AddHUDText("\"" + Text.StuffMakeThingGood.Get() + "\"");
            }
            else
            {
                Utility.AddHUDText(Text.RemedyApplied.Get());
            }
        }


        /// <summary>
        /// Removes all the ingredients needed for the remedy from PC inventory.
        /// </summary>
        private static void RemoveIngredients(Remedy remedy)
        {
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;

            foreach (int templateIndex in remedy.Ingredients)
            {
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (!item.IsQuestItem && item.IsOfTemplate(templateIndex))
                        playerItems.RemoveOne(item);
                }
            }
        }


        /// <summary>
        /// Attached remedy incumbent effect to the patient.
        /// </summary>
        private static void ApplyRemedyEffect(Remedy remedy)
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();

            int remedyIndex = Remedies.LastIndexOf(remedy);
            settings.ChanceBase = remedyIndex;

            EntityEffectManager manager = patient.GetComponent<EntityEffectManager>();

            EntityEffectBundle bundle = manager.CreateSpellBundle(HerbalRemedy.HerbalEffectKey, ElementTypes.Poison, settings);
            bundle.CasterEntityBehaviour = player;
            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;

            manager.AssignBundle(bundle, flags);
        }



        private const int poisonsOffset = 128;
        private static readonly HashSet<Poisons> expungable = new HashSet<Poisons>() {
            Poisons.Nux_Vomica, Poisons.Arsenic, Poisons.Moonseed, Poisons.Pyrrhic_Acid
        };

        /// <summary>
        /// Reduces poison time based on herbalist medical skill.
        /// Returns the number of poison rounds remaining.
        /// </summary>
        private static int TreatPoison(bool cleansing)
        {
            float medical = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            float potency = 0.1f + medical / 100f;

            EntityEffectManager manager = patient.GetComponent<EntityEffectManager>();

            int totalRoundsRemaining = 0;

            foreach (LiveEffectBundle bundle in manager.EffectBundles)
            {
                foreach (IEntityEffect effect in bundle.liveEffects)
                {
                    if (!(effect is PoisonEffect))
                        continue;

                    PoisonEffect poisonEffect = effect as PoisonEffect;

                    Poisons variant = (Poisons)(poisonsOffset + poisonEffect.CurrentVariant);

                    bool treatable = cleansing ^ expungable.Contains(variant);
                    if (!treatable)
                        continue;

                    PoisonEffect.SaveData_v1 data = (PoisonEffect.SaveData_v1)poisonEffect.GetSaveData();

                    float reduction = data.minutesRemaining * (potency / 100) + 1;
                    reduction = Mathf.Clamp(reduction, 1f, 500f);
                    if (reduction > data.minutesRemaining)
                        reduction = data.minutesRemaining;

                    data.minutesRemaining -= (int)reduction;

                    totalRoundsRemaining += data.minutesRemaining;

                    poisonEffect.RestoreSaveData(data);
                }

            }

            return totalRoundsRemaining;

        }


        /// <summary>
        /// Enters player into envenomed state incubent effect.
        /// </summary>
        private static void EnterEnvenomedState(Remedy remedy)
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();

            int remedyIndex = Remedies.LastIndexOf(remedy);
            settings.ChanceBase = remedyIndex;
            
            EntityEffectManager manager = player.GetComponent<EntityEffectManager>();

            EntityEffectBundle bundle = manager.CreateSpellBundle(Envenomed.EnvenomedEffectKey, ElementTypes.None, settings);
            bundle.CasterEntityBehaviour = player;
            
            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;

            manager.AssignBundle(bundle, flags);
        }


        /// <summary>
        /// Removes envenomed incumbent effect from player.
        /// This is needed in cases where the player attempts another herbal remedy while
        /// envenomed status is active.
        /// </summary>
        private static void ExitEnvenomedState()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;
            EntityEffectManager manager = player.GetComponent<EntityEffectManager>();

            foreach (LiveEffectBundle bundle in manager.EffectBundles)
                foreach (IEntityEffect effect in bundle.liveEffects)
                    if (effect.Key.Equals(Text.Envenomed.Get()))
                    {
                        Envenomed envenomed = effect as Envenomed;
                        envenomed.ExitState();
                        return;
                    }
        }


        /// <summary>
        /// Checks if a remedy is actually needed by the patient.
        /// </summary>
        private static bool IsRemedyNeeded(Remedy remedy)
        {
            EntityEffectManager manager = patient.GetComponent<EntityEffectManager>();

            switch (remedy.Name)
            {
                case Text.RecoverFatigue:
                    return patient.Entity.CurrentFatigue < patient.Entity.MaxFatigue;
                case Text.ResistParalysis:
                    return true;
                case Text.RegenerateHealth:
                    return patient.Entity.CurrentHealth < patient.Entity.MaxHealth;
                case Text.RecoverMagicka:
                    return patient.Entity.CurrentMagicka < patient.Entity.MaxMagicka;
                case Text.ExpungePoison:
                case Text.CleansePoison:
                    bool cleansing = remedy.Name.Equals(Text.CleansePoison);

                    foreach (LiveEffectBundle bundle in manager.EffectBundles)
                    {
                        foreach (IEntityEffect effect in bundle.liveEffects)
                        {
                            if (!(effect is PoisonEffect))
                                continue;

                            PoisonEffect poisonEffect = effect as PoisonEffect;

                            Poisons variant = (Poisons)(poisonsOffset + poisonEffect.CurrentVariant);

                            bool treatable = cleansing ^ expungable.Contains(variant);
                            if (!treatable)
                                continue;

                            PoisonEffect.SaveData_v1 data = (PoisonEffect.SaveData_v1)poisonEffect.GetSaveData();
                            if (data.currentState == PoisonEffect.PoisonStates.Active)
                            {
                                string poisonName = variant.ToString().Replace('_', ' ');
                                string diagnosis = Text.PatientSuffersFromPoisoning.Get(poisonName);
                                if (patient.EntityType == EntityTypes.Player)
                                    diagnosis = Text.YouSufferFromPoisoning.Get(poisonName);

                                Utility.AddHUDText(diagnosis);
                                return true;
                            }

                        }
                    }
                    return false;
                case Text.Moonseed:
                case Text.Magebane:
                case Text.PyrrhicAcid:
                    return true;
                default:
                    if (remedy.Stat != DFCareer.Stats.None)
                    {
                        int currentStatValue = patient.Entity.Stats.GetLiveStatValue(remedy.Stat);
                        int normalStatValue = patient.Entity.Stats.GetPermanentStatValue(remedy.Stat);
                        return currentStatValue < normalStatValue;
                    }
                    else
                    {
                        return false;
                    }
            }

        }


        /// <summary>
        /// Applies appropriate side effect for the remedy used.
        /// </summary>
        private static void ApplySideEffects(Remedy remedy)
        {
            int value = Random.Range(8, 28);

            switch (remedy.TreatmentDescription)
            {
                case Text.TreatLethargyViaExhilarantTonic:
                case Text.TreatLethargyViaErgogenicInfusion:
                case Text.Envenomed:
                    //no side effects
                    break;
                case Text.TreatToxaemiaViaRenalExpungement:
                case Text.TreatKinesiaViaCalomel:
                    patient.Entity.DecreaseMagicka(value);
                    break;
                case Text.TreatDeliriumViaBotanicalCordial:
                case Text.TreatAmentiaViaBotanicalEnema:
                    patient.Entity.DecreaseHealth(value); //ouchy
                    break;
                case Text.TreatSalubriousAccelerantViaBotanicalTincture:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                case Text.TreatFluxePaucityViaMetallurgicCordial:
                case Text.TreatFluxePaucityViaDraconicIncense:
                case Text.TreatToxaemiaViaDiaphoreticDepurative:
                case Text.TreatAtrophiaViaAnapleroticTincture:
                case Text.TreatAtrophiaViaAnapleroticUnction:
                case Text.TreatPhrenitisViaAntiphlogisticTonic:
                case Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion:
                case Text.TreatAtaxiaViaAntispasmodicEnema:
                case Text.TreatAtaxiaViaAntispasmodicSalve:
                case Text.TreatAnaemiaViaHepaticDepurative:
                case Text.TreatHepaticPhlegmasiaViaFloralIncense:
                case Text.TreatDistemperViaSoothingCordial:
                case Text.TreatEffluviaViaRectifyingDecoction:
                case Text.TreatCatylepsyViaAntiparalyticOintment:
                default:
                    patient.Entity.DecreaseFatigue(value, true);
                    break;
            }

        }



    }




    public class MortarAndPestle : DaggerfallUnityItem
    {
        public const int MortarAndPestleTemplateIndex = 1772;

        private const int baseValue = 35;    // Base gold value


        public MortarAndPestle() : this(baseValue)
        {
        }


        public MortarAndPestle(int baseValue) : base(ItemGroups.MiscellaneousIngredients2, MortarAndPestleTemplateIndex)
        {
            value = baseValue;
        }


        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(MortarAndPestle).ToString();
            return data;
        }


        public override bool IsStackable()
        {
            return false;
        }

        public override EquipSlots GetEquipSlot()
        {
            return GameManager.Instance.PlayerEntity.ItemEquipTable.GetFirstSlot(EquipSlots.Crystal0, EquipSlots.Crystal1);
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipBow;
        }


    } //class MortarAndPestle



    public class HerbalRemedy : IncumbentEffect
    {
        private Herbalism.Remedy remedy;

        public const string HerbalEffectKey = "Herbal-Remedy";

        public override string GroupName => remedy?.Name.Get();

        public override void SetProperties()
        {
            properties.Key = HerbalEffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
            properties.AllowedElements = ElementTypes.Poison;
            properties.AllowedCraftingStations = MagicCraftingStations.None;
            properties.DisableReflectiveEnumeration = true;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            InitRemedy();

            RoundsRemaining = CalculateDuration();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            InitRemedy();
        }


        public override void MagicRound()
        {
            base.MagicRound();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            float medical = Caster == null ? 20 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            float potency = 0.1f + medical / 100.0f;

            switch (remedy.TreatmentDescription)
            {
                case Text.TreatLethargyViaExhilarantTonic:
                case Text.TreatLethargyViaErgogenicInfusion:
                    entityBehaviour.Entity.IncreaseFatigue((int)(potency * 16), true);
                    break;
                case Text.TreatSalubriousAccelerantViaBotanicalTincture:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                    entityBehaviour.Entity.IncreaseHealth((int)(potency * 14));
                    break;
                case Text.TreatFluxePaucityViaMetallurgicCordial:
                case Text.TreatFluxePaucityViaDraconicIncense:
                    entityBehaviour.Entity.IncreaseMagicka((int)(potency * 14));
                    break;
                default:
                    manager.HealAttribute(remedy.Stat, 1 + (int)(potency * 3));
                    break;
            }

        }

        public override void ConstantEffect()
        {
            base.ConstantEffect();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            //paralysis immunity is reset each frame, and must be reapplied
            if (remedy.Name == Text.ResistParalysis)
                entityBehaviour.Entity.IsImmuneToParalysis = true;
        }


        public override void End()
        {
            base.End();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            if (remedy.TreatmentDescription == Text.TreatCatylepsyViaAntiparalyticOintment)
                entityBehaviour.Entity.IsImmuneToParalysis = false;
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            //ChanceBase contains the Remedies index
            return other is HerbalRemedy && other.Settings.ChanceBase == settings.ChanceBase;
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];

            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += CalculateDuration();
        }


        private void InitRemedy()
        {
            ParentBundle.icon.index = 41; //blue-green puff
            if (DaggerfallUI.Instance.SpellIconCollection.HasPack("D.R.E.A.M. Icons"))
            {
                ParentBundle.icon.key = "D.R.E.A.M. Icons";
                ParentBundle.icon.index = 220; //green leaves
            }

            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];
        }


        private int CalculateDuration()
        {
            int medical = Caster == null ? 25 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            switch (remedy.TreatmentDescription)
            {
                //The second version of various remedies are longer lasting
                case Text.TreatLethargyViaErgogenicInfusion:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                case Text.TreatFluxePaucityViaDraconicIncense:
                case Text.TreatAtrophiaViaAnapleroticUnction:
                case Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion:
                case Text.TreatAmentiaViaBotanicalEnema:
                case Text.TreatAtaxiaViaAntispasmodicSalve:
                case Text.TreatHepaticPhlegmasiaViaFloralIncense:
                case Text.TreatEffluviaViaRectifyingDecoction:
                case Text.TreatCatylepsyViaAntiparalyticOintment:
                    return medical / 2;
                default:
                    return medical / 3;
            }
        }


    } //class HerbalRemedy



    public class Envenomed : IncumbentEffect
    {
        private Herbalism.Remedy remedy;

        public const string EnvenomedEffectKey = "Envenomed";

        public override string GroupName => remedy?.Name.Get();

        public override void SetProperties()
        {
            properties.Key = EnvenomedEffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = ElementTypes.None;
            properties.AllowedCraftingStations = MagicCraftingStations.None;
            properties.DisableReflectiveEnumeration = true;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            //Duration
            int medical = Caster == null ? 30 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
            RoundsRemaining = medical / 4;

            EnvenomWeapons();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            EnvenomWeapons();
        }


        public override void MagicRound()
        {
            base.MagicRound();

            //The poison is applied/reapplied each round
            EnvenomWeapons();
        }

        public void ExitState()
        {
            ResignAsIncumbent();
            RoundsRemaining = 0;
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            //the incumbent effect will resign and be replaced by this effect
            return false; 
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            //the incumbent effect will resign and be replaced by this effect
        }


        /// <summary>
        /// Sets the poisonType value for all shortblade and bow items in PC inventory.
        /// </summary>
        private void EnvenomWeapons()
        {
            ParentBundle.icon.index = 67; //greenish streak
            if (DaggerfallUI.Instance.SpellIconCollection.HasPack("D.R.E.A.M. Icons"))
            {
                ParentBundle.icon.key = "D.R.E.A.M. Icons";
                ParentBundle.icon.index = 100; //green dagger
            }

            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];

            Poisons poisonType;
            if (remedy.Name.Equals(Text.Magebane))
                poisonType = Poisons.Magebane;
            else if (remedy.Name.Equals(Text.PyrrhicAcid))
                poisonType = Poisons.Pyrrhic_Acid;
            else
                poisonType = Poisons.Moonseed;


            //Apply poison to short blades and bows
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
            for (int i = 0; i < playerItems.Count; ++i)
            {
                DaggerfallUnityItem item = playerItems.GetItem(i);
                int skill = item.GetWeaponSkillUsed();
                if (skill == (int)DFCareer.ProficiencyFlags.ShortBlades || skill == (int)DFCareer.ProficiencyFlags.MissileWeapons)
                {
                    item.poisonType = poisonType;
                }
            }
        }


    } //class Envenomed



} //namespace