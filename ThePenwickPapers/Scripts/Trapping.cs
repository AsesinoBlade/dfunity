// Project:     Trapping, The Penwick Papers for Daggerfall Unity
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
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;



namespace ThePenwickPapers
{

    public class Trapping
    {
        private class TrapType
        {
            public int Index;
            public Text Name;
            public int RequiredSkill;
            public int Reliability;
            public SoundClips[] Sounds;
            public int[] Ingredients;

            public TrapType(int index, Text name, int requiredSkill, int reliability, SoundClips[] sounds, int[] ingredients)
            {
                Index = index;
                Name = name;
                RequiredSkill = requiredSkill;
                Reliability = reliability;
                Sounds = sounds;
                Ingredients = ingredients;
            }
        }

        private static readonly List<TrapType> trapTypes = new List<TrapType>()
        {
            new TrapType(0, Text.Snaring, 18, 70,
                new SoundClips[] {SoundClips.EquipLeather, SoundClips.EquipLeather},
                new int[] {(int)PlantIngredients1.Root_tendrils}),

            new TrapType(1, Text.Snaring, 28, 85,
                new SoundClips[] {SoundClips.EquipClothing, SoundClips.EquipClothing},
                new int[] {(int)CreatureIngredients2.Mummy_wrappings}),

            new TrapType(2, Text.Venomous, 22, 75,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Snake_venom, (int)PlantIngredients2.Bamboo}),

            new TrapType(3, Text.Venomous, 31, 80,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Snake_venom, (int)MiscellaneousIngredients1.Medium_tooth}),

            new TrapType(4, Text.Venomous, 35, 90,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Snake_venom, (int)MiscellaneousIngredients1.Big_tooth}),

            new TrapType(5, Text.Paralyzing, 24, 70,
                new SoundClips[] {SoundClips.EquipLeather, SoundClips.EquipLeather, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients2.Small_scorpion_stinger, (int)PlantIngredients1.Root_tendrils}),

            new TrapType(6, Text.Paralyzing, 39, 80,
                new SoundClips[] {SoundClips.EquipLeather, SoundClips.EquipClothing, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients2.Small_scorpion_stinger, (int)CreatureIngredients2.Mummy_wrappings}),

            new TrapType(7, Text.Paralyzing, 27, 75,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Spider_venom, (int)PlantIngredients2.Bamboo}),

            new TrapType(8, Text.Paralyzing, 33, 85,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Spider_venom, (int)MiscellaneousIngredients1.Medium_tooth}),

            new TrapType(9, Text.Paralyzing, 41, 95,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)CreatureIngredients1.Spider_venom, (int)MiscellaneousIngredients1.Big_tooth}),

            new TrapType(10, Text.Paralyzing, 34, 80,
                new SoundClips[] {SoundClips.EquipLeather, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients2.Giant_scorpion_stinger, (int)PlantIngredients1.Root_tendrils}),

            new TrapType(11, Text.Paralyzing, 45, 95,
                new SoundClips[] {SoundClips.EquipLeather, SoundClips.EquipClothing, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients2.Giant_scorpion_stinger, (int)CreatureIngredients2.Mummy_wrappings}),

            new TrapType(12, Text.FlamingBomb, 37, 90,
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)UselessItems2.Oil, (int)PlantIngredients2.Root_bulb, (int)MetalIngredients.Sulphur}),

            new TrapType(13, Text.LunaStick, 15, 100,
                new SoundClips[] { SoundClips.AmbientWaterBubbles, SoundClips.ActivateGears},
                new int[] {(int)CreatureIngredients1.Ectoplasm, (int)PlantIngredients1.Twigs})
        };

        private const string trapObjectNamePrefix = "Menacing Trap";

        private static DaggerfallListPickerWindow trapPicker;
        private static bool layingTrap;
        private static Vector3 trapLocation;


        /// <summary>
        /// Called when player activates something while in 'Steal' mode.
        /// Checks location and conditions to see if a trap can be set.
        /// If so, then shows the trap selection listbox.
        /// </summary>
        public static bool AttemptLayTrap(RaycastHit hitInfo)
        {
            if (hitInfo.collider == null)
                return false;    //must activate terrain
            else if (hitInfo.distance > 2.5)
                return false;
            else if (!GameManager.Instance.PlayerMotor.IsCrouching)
                return false;

            bool hitTerrain = hitInfo.collider is TerrainCollider || hitInfo.collider is MeshCollider;
            if (!hitTerrain)
                return false;

            if (Vector3.Angle(hitInfo.normal, Vector3.up) > 30)
                return false; //must hit floor, some slope allowed

            if (layingTrap)
                return true;

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

            trapLocation = hitInfo.point;

            ShowTrapPicker();

            return true;
        }


        /// <summary>
        /// Called when a game is loaded.  Sets the trigger on any existing traps in the player's
        /// current location.
        /// </summary>
        public static void OnLoadEvent()
        {
            layingTrap = false;

            DaggerfallLoot[] lootMarkers = Object.FindObjectsOfType<DaggerfallLoot>();

            foreach (DaggerfallLoot loot in lootMarkers)
            {
                if (loot.entityName.StartsWith(trapObjectNamePrefix))
                    SetTrigger(loot);
            }
        }


        /// <summary>
        /// Shows the listbox of available traps that the PC has the skill to set.
        /// </summary>
        private static void ShowTrapPicker()
        {
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;

            int lockpicking = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);

            trapPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            trapPicker.OnItemPicked += TrapPicker_OnItemPicked;
            trapPicker.OnCancel += TrapPicker_OnCancel;

            ListBox listBox = trapPicker.ListBox;
            ListBox.ListItem item;
            int count = 0;
            listBox.SelectNone();

            foreach (TrapType trap in trapTypes)
            {
                if (trap.RequiredSkill > lockpicking)
                    continue;

                ++count;

                StringBuilder str = new StringBuilder();
                str.Append(trap.Name.Get());
                str.Append(" (");
                for (int i = 0; i < trap.Ingredients.Length; ++i)
                {
                    if (i > 0)
                        str.Append(", ");

                    ItemTemplate template = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(trap.Ingredients[i]);
                    str.Append(template.name);
                }
                str.Append(")");

                listBox.AddItem(str.ToString(), out item, -1, trap);

                item.tag = trap;

                if (!HasIngredients(trap, playerItems))
                {
                    item.textColor = new Color(0.8f, 0.8f, 0.8f);
                    item.highlightedTextColor = item.textColor;
                }
            }

            if (count == 0)
            {
                string skillName = DaggerfallUnity.Instance.TextProvider.GetSkillName(DFCareer.Skills.Lockpicking);
                Utility.AddHUDText(Text.TrapsUnknown.Get(skillName));
                return;
            }

            uiManager.PushWindow(trapPicker);
        }


        /// <summary>
        /// Checks if the PC has all the items needed to set the specified trap.
        /// </summary>
        private static bool HasIngredients(TrapType trap, ItemCollection items)
        {
            foreach (int index in trap.Ingredients)
            {
                bool found = false;
                for (int i = 0; i < items.Count; i++)
                {
                    DaggerfallUnityItem item = items.GetItem(i);
                    if (!item.IsQuestItem && item.IsOfTemplate(index))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }

            return true;
        }


        /// <summary>
        /// Triggered when an item is selected in the trap list box.
        /// Starts the trap laying coroutine.
        /// </summary>
        private static void TrapPicker_OnItemPicked(int index, string itemText)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            //The trap picker window prevented 'Action Complete' from being seen, so we must
            //manually stop swallowing actions
            ThePenwickPapersMod.StopSwallowingActions();

            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;

            ListBox.ListItem item = trapPicker.ListBox.GetItem(index);
            TrapType trap = (TrapType)item.tag;

            if (!HasIngredients(trap, playerItems))
            {
                Utility.AddHUDText(Text.TrapIngredientsNotInInventory.Get());
                return;
            }

            IEnumerator coroutine = LayTrapCoroutine(trap);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Triggered when the Escape key is pressed to cancel the list picker window
        /// </summary>
        private static void TrapPicker_OnCancel(DaggerfallPopupWindow sender)
        {
            //The trap picker window prevented 'Action Complete' from being seen, so we must
            //manually stop swallowing actions
            ThePenwickPapersMod.StopSwallowingActions();
        }


        /// <summary>
        /// Coroutine that plays the trap-making sounds and calls CreateTrapObject.
        /// </summary>
        private static IEnumerator LayTrapCoroutine(TrapType trap)
        {
            layingTrap = true;

            Camera camera = GameManager.Instance.MainCamera;

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;

            DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();

            foreach (SoundClips sound in trap.Sounds)
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
                        Utility.AddHUDText(Text.TrappingInterrupted.Get());
                        dfAudioSource.AudioSource.Stop();
                        layingTrap = false;
                        yield break;
                    }
                    yield return new WaitForSeconds(tick);
                }
            }

            if (trap.Name == Text.LunaStick)
            {
                float potency = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
                potency /= 10;
                ApplyEffectBundle(GameManager.Instance.PlayerEntityBehaviour, LunaStick.EffectKey, ElementTypes.None, potency);
            }
            else
            {
                CreateTrapObject(trap);
            }

            layingTrap = false;

            GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Lockpicking, 4);
        }


        /// <summary>
        /// Creates the trap as a loot container and initializes it.
        /// </summary>
        private static void CreateTrapObject(TrapType trapType)
        {
            // Create unique LoadID for save system
            ulong loadID = DaggerfallUnity.NextUID;

            DaggerfallLoot trapMarker = GameObjectHelper.CreateLootContainer(
                LootContainerTypes.RandomTreasure,
                InventoryContainerImages.Ground,
                trapLocation,
                GameObjectHelper.GetBestParent(),
                216,
                38,
                loadID
                );

            trapMarker.entityName = trapObjectNamePrefix; //this indicates loot container is a penwick trap
            trapMarker.stockedDate = trapType.Index; //using stockedDate to store the trap type
            trapMarker.playerOwned = true;
            trapMarker.customDrop = true;

            TransferIngredients(trapType, trapMarker);

            SetTrigger(trapMarker);
        }


        /// <summary>
        /// Adds the trap ingredients to the trap loot marker.
        /// The player can later retrieve the items from an untriggered trap, thereby deactivating it.
        /// </summary>
        private static void TransferIngredients(TrapType trapType, DaggerfallLoot trapMarker)
        {
            foreach (int templateIndex in trapType.Ingredients)
            {
                DaggerfallUnityItem item = RetrieveIngredient(templateIndex);
                trapMarker.Items.AddItem(item);
            }
        }


        /// <summary>
        /// Removes the ingredient of the specified template index from PC inventory and returns it.
        /// Returns null if the ingredient is not in inventory.
        /// </summary>
        private static DaggerfallUnityItem RetrieveIngredient(int templateIndex)
        {
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;

            for (int i = 0; i < playerItems.Count; i++)
            {
                DaggerfallUnityItem item = playerItems.GetItem(i);
                if (!item.IsQuestItem && item.IsOfTemplate(templateIndex))
                {
                    playerItems.RemoveOne(item);
                    DaggerfallUnityItem newItem = new DaggerfallUnityItem(item);
                    newItem.stackCount = 1;
                    return newItem;
                }
            }

            return null;
        }


        /// <summary>
        /// Adds a collider trigger to the trap to detect when something hits it.
        /// </summary>
        private static void SetTrigger(DaggerfallLoot trapMarker)
        {
            trapMarker.gameObject.AddComponent<TrapTriggerDetection>();
        }


        /// <summary>
        /// Called by the trap's trigger collider when an entity steps on the trap.
        /// Checks if the trap was successfully triggered.  If so, calls ShowTriggerAnimation() and
        /// ApplyTrapEffects().
        /// </summary>
        private static void TriggerTrap(DaggerfallLoot trapMarker, TrapType trapType, DaggerfallEntityBehaviour victim)
        {
            if (!HasIngredients(trapType, trapMarker.Items))
            {
                //if one or more ingredients were removed from the trap, the trap won't trigger
                return;
            }

            if (victim.EntityType == EntityTypes.EnemyMonster)
            {
                EnemyEntity enemy = victim.Entity as EnemyEntity;
                if (enemy.MobileEnemy.Weight < 20)
                {
                    //small or intangible enemies won't trigger trap
                    return;
                }
            }

            int chance = trapType.Reliability;

            if (victim.EntityType == EntityTypes.Player)
            {
                //players are less likely to trigger the trap that they set
                chance -= GameManager.Instance.PlayerEntity.Stats.LiveLuck;
            }

            if (Dice100.SuccessRoll(chance))
            {
                ShowTriggerAnimation(trapMarker, trapType);

                ApplyTrapEffects(trapType, victim);
            }
        }


        /// <summary>
        /// Called after trap is triggered.
        /// Creates on-shot billboard for effect animation, then destroys the trap.
        /// </summary>
        private static void ShowTriggerAnimation(DaggerfallLoot trapMarker, TrapType trapType)
        {
            int archive;
            int record;
            int framesPerSecond = 15;
            SoundClips clip;

            switch (trapType.Name)
            {
                case Text.Venomous:
                    archive = 377;
                    record = 1;
                    clip = SoundClips.SpellImpactPoison;
                    break;
                case Text.Paralyzing:
                    archive = 378;
                    record = 1;
                    clip = SoundClips.EnemyScorpionAttack;
                    break;
                case Text.FlamingBomb:
                    archive = 375;
                    record = 1;
                    clip = SoundClips.SpellImpactFire;
                    break;
                default:
                    archive = 380;
                    record = 2;
                    framesPerSecond = 10;
                    clip = SoundClips.EquipLeather;
                    break;
            }

            GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(archive, record, null);
            go.name = "Penwick Trap Splash";
            go.transform.position = trapMarker.transform.position + Vector3.up * 0.1f;
            DaggerfallAudioSource audio = go.AddComponent<DaggerfallAudioSource>();
            audio.PlayOneShot(clip);
            Billboard billboard = go.GetComponent<Billboard>();
            billboard.FramesPerSecond = framesPerSecond;
            billboard.FaceY = true;
            billboard.OneShot = true;
            billboard.GetComponent<MeshRenderer>().receiveShadows = false;

            Object.Destroy(trapMarker.gameObject);
        }


        /// <summary>
        /// Applies trap effects by creating an effect bundle and applying it to the victim.
        /// </summary>
        private static void ApplyTrapEffects(TrapType trapType, DaggerfallEntityBehaviour victim)
        {
            float lockpicking = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);

            float potency = 0.1f + lockpicking / 100f;

            switch (trapType.Name)
            {
                case Text.Snaring:
                    IEnumerator coroutine = Snare(victim, potency);
                    ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
                    break;
                case Text.Venomous:
                    ApplyEffectBundle(victim, ContinuousDamageHealth.EffectKey, ElementTypes.Poison, potency);
                    break;
                case Text.Paralyzing:
                    ApplyEffectBundle(victim, Paralyze.EffectKey, ElementTypes.Poison, potency / 3f);
                    break;
                case Text.FlamingBomb:
                    ApplyEffectBundle(victim, ContinuousDamageHealth.EffectKey, ElementTypes.Fire, potency);
                    break;
                default:
                    return;
            }

        }


        /// <summary>
        /// Initializes the EffectSettings and assigns the bundle.
        /// </summary>
        private static void ApplyEffectBundle(DaggerfallEntityBehaviour victim, string effectKey, ElementTypes element, float potency)
        {
            EntityEffectManager manager = victim.GetComponent<EntityEffectManager>();

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();

            settings.DurationBase = 1 + (int)(8 * potency);
            settings.DurationPlus = 0;
            settings.DurationPerLevel = 1;

            settings.ChanceBase = 100;

            //trap damage is scaled by level
            settings.MagnitudeBaseMin = (int)(5 * potency);
            settings.MagnitudeBaseMax = (int)(10 * potency);
            settings.MagnitudePlusMin = (int)(3 * potency);
            settings.MagnitudePlusMax = (int)(9 * potency);
            settings.MagnitudePerLevel = 1;

            EntityEffectBundle bundle = manager.CreateSpellBundle(effectKey, element, settings);
            AssignBundleFlags flags = AssignBundleFlags.BypassChance;
            if (element == ElementTypes.None)
                flags |= AssignBundleFlags.BypassSavingThrows;

            manager.AssignBundle(bundle, flags);
        }


        /// <summary>
        /// Coroutine activated on triggered snares that pulls the victim backward if it moves
        /// too far from the trap location.
        /// The coroutine ends after a length of time determined by trap potency and victim strength.
        /// PCs are not affected by snares.
        /// </summary>
        private static IEnumerator Snare(DaggerfallEntityBehaviour victim, float potency)
        {
            if (victim.EntityType == EntityTypes.Player)
                yield break;

            EnemyMotor motor = victim.GetComponent<EnemyMotor>();

            Vector3 snarePos = victim.transform.position;

            EnemyEntity enemy = victim.Entity as EnemyEntity;
            float potencyReducer = Mathf.Clamp(enemy.Stats.LiveStrength * 0.0001f, 0.001f, 0.02f);

            while (potency > 0)
            {
                potency -= potencyReducer;
                Vector3 pullDirection = (snarePos - victim.transform.position).normalized;
                float distance = Vector3.Distance(snarePos, victim.transform.position);
                if (distance > 1)
                {
                    motor.KnockbackDirection = pullDirection;
                    motor.KnockbackSpeed = distance * 5;
                }

                yield return new WaitForSeconds(0.2f);
            }

        }


        public class TrapTriggerDetection : MonoBehaviour
        {
            void Start()
            {
            }

            void OnTriggerEnter(Collider other)
            {
                DaggerfallLoot trapMarker = GetComponent<DaggerfallLoot>();

                TrapType triggeredTrapType = null;

                foreach (TrapType trapType in Trapping.trapTypes)
                {
                    if (trapMarker.stockedDate == trapType.Index)
                    {
                        triggeredTrapType = trapType;
                        break;
                    }
                }

                DaggerfallEntityBehaviour victim = other.GetComponent<DaggerfallEntityBehaviour>();

                if (victim && triggeredTrapType != null)
                {
                    Trapping.TriggerTrap(trapMarker, triggeredTrapType, victim);
                }

            }

        } //class TriggerTrapDetection



    } //class Trapping



    public class LunaStick : IncumbentEffect
    {
        private GameObject lunaStick;

        public const string EffectKey = "Luna-Stick";

        public override string GroupName => EffectKey;

        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = ElementTypes.None;
            properties.AllowedCraftingStations = MagicCraftingStations.None;
            properties.DisableReflectiveEnumeration = true;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            Init();

            RoundsRemaining = CalculateDuration();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            Init();
        }


        public override void MagicRound()
        {
            base.MagicRound();

            if (RoundsRemaining < 8)
                lunaStick.GetComponent<Light>().range = RoundsRemaining + 1;
        }

        public override void ConstantEffect()
        {
            base.ConstantEffect();

            // Keep light positioned on top of player
            if (lunaStick)
                lunaStick.transform.position = GameManager.Instance.PlayerObject.transform.position;
        }


        public override void End()
        {
            base.End();

            if (lunaStick)
                GameObject.Destroy(lunaStick);
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return other is LunaStick;
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += CalculateDuration();
        }


        private void Init()
        {
            ParentBundle.icon.index = 62; //crescent moon
            if (DaggerfallUI.Instance.SpellIconCollection.HasPack("D.R.E.A.M. Icons"))
            {
                ParentBundle.icon.key = "D.R.E.A.M. Icons";
                ParentBundle.icon.index = 3; //green glow ball
            }

            //create light source
            lunaStick = new GameObject(Text.LunaStick.Get());
            lunaStick.transform.parent = GameObjectHelper.GetBestParent();
            Light myLight = lunaStick.AddComponent<Light>();
            myLight.type = LightType.Point;
            myLight.color = new Color32(158, 240, 180, 255);
            myLight.range = 8;
            lunaStick.SetActive(true);
        }


        private int CalculateDuration()
        {
            int trapping = Caster == null ? 25 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);

            return trapping;
        }


    } //class LunaStickEffect




} //namespace
