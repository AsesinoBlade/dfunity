// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Formulas;

namespace ThePenwickPapers
{
    public class ThePenwickPapersMod : MonoBehaviour, IHasModSaveData
    {
        private static Mod mod;
        private static string missingTextSubstring;

        public static ThePenwickPapersMod Instance;
        public static SaveData Persistent = new SaveData();
        public static FPSWeapon TheBoot;

        private int potionOfSeekingRecipeKey;
        private bool swallowActions = false;
        private PlayerActivateModes storedMode;
        private int modeSwitchCountdown = 0;

        //mod settings
        private bool enableEnhancedInfo;
        private bool enableTrapping;
        private bool enableHerbalism;
        private bool mouse3Check;
        private PlayerActivateModes mouse3Mode;
        private bool mouse4Check;
        private PlayerActivateModes mouse4Mode;
        private bool enableTeleportMinions;
        private bool startGameWithPotionOfSeeking;
        private bool enableGoverningAttributes;
        private int skillPerLevel;

        //assets
        public Texture2D SummoningEggTexture;
        public Texture2D PeepHoleTexture;
        public Texture2D PeepSlitTexture;
        public Texture2D GrapplingHookTexture;
        public Texture2D RopeTexture;
        public AudioClip WarpIn;
        public AudioClip ReanimateWarp;
        public AudioClip MaleOi;
        public AudioClip FemaleLaugh;
        public AudioClip MaleBreath;
        public AudioClip FemaleBreath;
        public AudioClip Creak1;
        public AudioClip Creak2;
        public AudioClip WindNoise;



        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<ThePenwickPapersMod>();
            go.name = "The Penwick Papers";

            //support for DirtyTricks/TheBoot
            TheBoot = go.AddComponent<FPSWeapon>();
            TheBoot.WeaponType = WeaponTypes.Melee;
            TheBoot.ShowWeapon = false;

            missingTextSubstring = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.NoTranslationFoundMessage.Substring(0, 14);

            mod.IsReady = true;
        }


        /// <summary>
        /// Gets string corresponding to provided key from textdatabase.txt
        /// </summary>
        public static string GetText(string key)
        {
            return mod.Localize(key);
        }


        /// <summary>
        /// Checks if specified key actually exists in textdatabase.txt
        /// </summary>
        public static bool TextExists(string key)
        {
            string result = GetText(key);

            return !result.StartsWith(missingTextSubstring);
        }


        /// <summary>
        /// This is called after list picker windows are shown to manually stop swalling activate actions.
        /// </summary>
        public static void StopSwallowingActions()
        {
            Instance.swallowActions = false;
        }


        public int GetPotionOfSeekingRecipeKey()
        {
            return potionOfSeekingRecipeKey;
        }


        public Type SaveDataType
        {
            get { return typeof(SaveData); }
        }

        public object NewSaveData()
        {
            return new SaveData();
        }

        public object GetSaveData()
        {
            return Persistent;
        }

        public void RestoreSaveData(object obj)
        {
            Persistent = (SaveData)obj;
        }


        private void Start()
        {
            Debug.Log("Start(): ThePenwickPapers");

            Instance = this;

            mod.SaveDataInterface = this;

            InitModSettings();

            //Make sure all localization text keys have entries in textdatabase.txt
            if (TextExtension.CheckTextKeysValid())
                Debug.Log("All localization text keys are valid");
            else
                Debug.LogWarning("Some localization text keys were not valid");


            //event handler registration
            StartGameBehaviour.OnStartGame += StartGameBehaviour_OnStartGameHandler;
            SaveLoadManager.OnStartLoad += SaveLoadManager_OnStartLoadHandler;
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoadHandler;
            PlayerDeath.OnPlayerDeath += PlayerDeath_OnPlayerDeathHandler;
            DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallRestWindow.OnSleepEnd += DaggerfallRestWindow_OnSleepEndHandler;
            PlayerEnterExit.OnTransitionExterior += PlayerEnterExit_OnTransitionExteriorHandler;
            PlayerEnterExit.OnTransitionDungeonExterior += PlayerEnterExit_OnTransitionDungeonExteriorHandler;
            GameManager.OnEnemySpawn += GameManager_OnEnemySpawnHandler;

            //load resources
            LoadTextures();
            LoadSounds();

            //Register the FormulaHelper overrides that are used for skill advancement if enabled
            if (enableGoverningAttributes)
            {
                FormulaHelper.RegisterOverride(mod, "CalculateSkillUsesForAdvancement",
                    (Func<int, int, float, int, int>)SkillAdvancement.CalculateSkillUsesForAdvancement);

                FormulaHelper.RegisterOverride(mod, "CalculatePlayerLevel",
                    (Func<int, int, int>)SkillAdvancement.CalculatePlayerLevel);

                SkillAdvancement.SkillPerLevel = skillPerLevel;
            }

            RegisterSpellsAndItems();

            Debug.Log("Finished Start(): ThePenwickPapers");
        }


        private void Update()
        {
            if (GameManager.IsGamePaused)
            {
                return; //Let's not hasten the heat-death of the universe
            }

            CheckForActivationAction();

            CheckUndeadInTown();

            if (DirtyTricks.EnableBlinding)
            {
                DirtyTricks.CheckEnemyBlindAttempt();
            }

            //control behavior of any summoned atronach/undead minions
            PenwickMinion.GuideMinions();
        }


        /// <summary>
        /// Gathers the settings data from the mod settings
        /// </summary>
        private void InitModSettings()
        {
            ModSettings modSettings = mod.GetSettings();

            //Features
            string featuresSection = "Features";

            enableEnhancedInfo = modSettings.GetBool(featuresSection, "EnhancedInfo");

            enableTrapping = modSettings.GetBool(featuresSection, "Trapping");

            enableHerbalism = modSettings.GetBool(featuresSection, "Herbalism");

            DirtyTricks.EnableBlinding = modSettings.GetBool(featuresSection, "DirtyTricks-Blinding");
            DirtyTricks.EnableChock = modSettings.GetBool(featuresSection, "DirtyTricks-Chock");
            DirtyTricks.EnableDiversion = modSettings.GetBool(featuresSection, "DirtyTricks-Diversion");
            DirtyTricks.EnableTheBoot = modSettings.GetBool(featuresSection, "DirtyTricks-TheBoot");
            DirtyTricks.EnablePeep = modSettings.GetBool(featuresSection, "DirtyTricks-Peep");

            mouse3Check = true;
            mouse4Check = true;
            switch (modSettings.GetInt(featuresSection, "Mouse3"))
            {
                case 1: mouse3Mode = PlayerActivateModes.Steal; break;
                case 2: mouse3Mode = PlayerActivateModes.Grab; break;
                case 3: mouse3Mode = PlayerActivateModes.Info; break;
                case 4: mouse3Mode = PlayerActivateModes.Talk; break;
                default: mouse3Check = false; break;
            }
            switch (modSettings.GetInt(featuresSection, "Mouse4"))
            {
                case 1: mouse4Mode = PlayerActivateModes.Steal; break;
                case 2: mouse4Mode = PlayerActivateModes.Grab; break;
                case 3: mouse4Mode = PlayerActivateModes.Info; break;
                case 4: mouse4Mode = PlayerActivateModes.Talk; break;
                default: mouse4Check = false; break;
            }

            //Options
            string optionsSection = "Options";

            enableTeleportMinions = modSettings.GetBool(optionsSection, "TeleportMinions");
            PenwickMinion.SetAutoTeleportMinions(enableTeleportMinions);

            startGameWithPotionOfSeeking = modSettings.GetBool(optionsSection, "StartGameWithPotionOfSeeking");


            //Advancement
            string advancementSection = "Advancement";
            enableGoverningAttributes = modSettings.GetBool(advancementSection, "GoverningAttributes");

            skillPerLevel = modSettings.GetInt(advancementSection, "SkillPerLevel");
        }



        /// <summary>
        /// Checks for player activation actions and passes control to appropriate modules.
        /// If no module can use the activation action, it is ignored and left to the system.
        /// </summary>
        private void CheckForActivationAction()
        {
            //Check if user-configured alternate mouse buttons have been used
            CheckMouseButtonOneShotActions();


            //swallow activate actions until mouse button is released
            if (swallowActions)
            {
                //ActionComplete is true after mouse button is released
                if (InputManager.Instance.ActionComplete(InputManager.Actions.ActivateCenterObject))
                {
                    swallowActions = false;
                }

                //This will prevent PlayerActivate from handling completed ActivateCenterObject
                //actions for a fraction of a second.
                GameManager.Instance.PlayerActivate.SetClickDelay(0.1f);

                return;
            }


            //This ActionStarted() check will trigger before the PlayerActivate ActionComplete() check
            //which gives us an opportunity to test and swallow events if needed.
            if (InputManager.Instance.ActionStarted(InputManager.Actions.ActivateCenterObject))
            {
                if (GameManager.Instance.PlayerEffectManager.ReadySpell != null)
                {
                    return;
                }
                else if (GameManager.Instance.PlayerMouseLook.cursorActive)
                {
                    return;
                }

                Camera camera = GameManager.Instance.MainCamera;
                RaycastHit hitInfo;
                int playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                float maxDistance = 16;
                bool hitSomething = Physics.Raycast(ray, out hitInfo, maxDistance, playerLayerMask);

                if (hitSomething)
                {
                    DaggerfallEntityBehaviour creature = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

                    bool actionHandled = false;

                    PlayerActivateModes mode = GameManager.Instance.PlayerActivate.CurrentMode;

                    if (enableEnhancedInfo && mode == PlayerActivateModes.Info)
                    {
                        actionHandled = EnhancedInfo.ShowEnhancedEntityInfo(hitInfo);
                    }

                    if (!actionHandled && enableHerbalism && mode == PlayerActivateModes.Grab)
                    {
                        //must be crouched and activating ground or ally/neutral
                        actionHandled = Herbalism.AttemptRemedy(hitInfo);
                    }

                    if (!actionHandled && enableTrapping && mode == PlayerActivateModes.Steal)
                    {
                        //must be crouched and activating ground
                        actionHandled = Trapping.AttemptLayTrap(hitInfo);
                    }

                    if (!actionHandled)
                    {
                        actionHandled = DirtyTricks.CheckAttemptTrick(hitInfo);
                    }

                    if (!actionHandled && mode == PlayerActivateModes.Grab)
                    {
                        actionHandled = GrapplingHook.AttemptHook(hitInfo);
                    }

                    if (!actionHandled && creature)
                    {
                        PenwickMinion minion = creature.GetComponent<PenwickMinion>();
                        actionHandled = minion && minion.Activate(hitInfo.distance);
                    }

                    if (actionHandled)
                    {
                        //We successfully handled this action, need to prevent PlayerActivate from seeing anything
                        swallowActions = true;
                    }
                }
            }
        }


        /// <summary>
        /// Checks if alternate mouse buttons have been clicked (mouse buttons 3 and 4)
        /// and handles the action by briefly switching to alternate interaction mode,
        /// performing the activation, then switching back over a span of multiple frames.
        /// </summary>
        private void CheckMouseButtonOneShotActions()
        {
            if (modeSwitchCountdown > 0)
            {
                --modeSwitchCountdown;
            }

            if (modeSwitchCountdown == 1)
            {
                //activation finished, switch back to previous mode
                GameManager.Instance.PlayerActivate.ChangeInteractionMode(storedMode, false);
            }

            if (mouse3Check && InputManager.Instance.GetKeyUp(KeyCode.Mouse3))
            {
                if (mouse3Mode != GameManager.Instance.PlayerActivate.CurrentMode)
                {
                    storedMode = GameManager.Instance.PlayerActivate.CurrentMode;
                    modeSwitchCountdown = 3;
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(mouse3Mode, false);
                }
                InputManager.Instance.AddAction(InputManager.Actions.ActivateCenterObject);
            }
            else if (mouse4Check && InputManager.Instance.GetKeyUp(KeyCode.Mouse4))
            {
                if (mouse4Mode != GameManager.Instance.PlayerActivate.CurrentMode)
                {
                    storedMode = GameManager.Instance.PlayerActivate.CurrentMode;
                    modeSwitchCountdown = 3;
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(mouse4Mode, false);
                }
                InputManager.Instance.AddAction(InputManager.Actions.ActivateCenterObject);
            }

        }


        /// <summary>
        /// Check if player is walking around town with reanimated undead, which is a felony is these parts.
        /// </summary>
        private void CheckUndeadInTown()
        {
            if (!GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
                return;  //either not in a town or inside a buliding

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
                return;

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            //if player already wanted for a crime, skip
            if (player.CrimeCommitted != 0)
                return;

            if (Dice100.FailedRoll(1))
                return;  //only checking occasionally


            //Player is in a town, outside, during the daytime.
            //Check if they have any undead minions.
            // "Oi! You gotta loicence for dat zombie?"
            PenwickMinion[] minions = PenwickMinion.GetMinions();
            foreach (PenwickMinion minion in minions)
            {
                EnemyEntity entity = minion.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity;
                if (entity.MobileEnemy.Affinity == MobileAffinity.Undead)
                {
                    //there officially is no 'Unlawful_Use_Of_Necromancy' crime in the books
                    player.CrimeCommitted = PlayerEntity.Crimes.Criminal_Conspiracy;
                    player.SpawnCityGuards(true);
                    break;
                }
            }

        }


        /// <summary>
        /// Loads the sound assets used by this mod
        /// </summary>
        private void LoadSounds()
        {
            ModManager modManager = ModManager.Instance;
            bool success = true;

            success &= modManager.TryGetAsset("WarpIn", false, out WarpIn);
            success &= modManager.TryGetAsset("ReanimateWarp", false, out ReanimateWarp);
            success &= modManager.TryGetAsset("MaleOi", false, out MaleOi);
            success &= modManager.TryGetAsset("FemaleLaugh", false, out FemaleLaugh);
            success &= modManager.TryGetAsset("MaleBreath", false, out MaleBreath);
            success &= modManager.TryGetAsset("FemaleBreath", false, out FemaleBreath);
            success &= modManager.TryGetAsset("Creak1", false, out Creak1);
            success &= modManager.TryGetAsset("Creak2", false, out Creak2);
            success &= modManager.TryGetAsset("WindNoise", false, out WindNoise);

            if (!success)
                throw new Exception("Missing sound asset");
        }


        /// <summary>
        /// Loads the texture assets needed by this mod.
        /// </summary>
        private void LoadTextures()
        {
            //using native game texture for summoning egg
            GetTextureResults results = GetBuiltinTexture(157, 1);
            SummoningEggTexture = results.albedoMap;
            //modify the alpha transparency
            Color32[] pixels = SummoningEggTexture.GetPixels32();
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].a = (byte)(255 - pixels[i].r);
            }
            SummoningEggTexture.SetPixels32(pixels);
            SummoningEggTexture.Apply(false);

            ModManager modManager = ModManager.Instance;
            bool success = true;
            success &= modManager.TryGetAsset("PeepHole", false, out PeepHoleTexture);
            success &= modManager.TryGetAsset("PeepSlit", false, out PeepSlitTexture);
            success &= modManager.TryGetAsset("GrapplingHook", false, out GrapplingHookTexture);
            success &= modManager.TryGetAsset("Rope", false, out RopeTexture);

            if (!success)
                throw new Exception("Missing texture asset");
        }


        /// <summary>
        /// Gets game texture as GetTextureResults given the archive and record.
        /// </summary>
        private GetTextureResults GetBuiltinTexture(int archive, int record)
        {
            GetTextureSettings settings = new GetTextureSettings();
            settings.archive = archive;
            settings.record = record;
            settings.frame = 0;
            settings.stayReadable = true;
            return DaggerfallUnity.Instance.MaterialReader.TextureReader.GetTexture2D(settings);
        }


        /// <summary>
        /// Registers magic effect templates and item templates.
        /// </summary>
        private void RegisterSpellsAndItems()
        {
            EntityEffectBroker effectBroker = GameManager.Instance.EntityEffectBroker;

            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(
                LandmarkJournalItem.LandmarkJournalTemplateIndex,
                ItemGroups.UselessItems2,
                typeof(LandmarkJournalItem));

            if (enableHerbalism)
            {
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(
                    MortarAndPestle.MortarAndPestleTemplateIndex,
                    ItemGroups.MiscellaneousIngredients2,
                    typeof(MortarAndPestle));

                HerbalRemedy herbalRemedyTemplateEffect = new HerbalRemedy();
                effectBroker.RegisterEffectTemplate(herbalRemedyTemplateEffect);

                Envenomed envenomedTemplateEffect = new Envenomed();
                effectBroker.RegisterEffectTemplate(envenomedTemplateEffect);
            }

            if (enableTrapping)
            {
                LunaStick lunaStickTemplateEffect = new LunaStick();
                effectBroker.RegisterEffectTemplate(lunaStickTemplateEffect);
            }

            CreateAtronach createAtronachTemplateEffect = new CreateAtronach();
            effectBroker.RegisterEffectTemplate(createAtronachTemplateEffect);

            Reanimate reanimateTemplateEffect = new Reanimate();
            effectBroker.RegisterEffectTemplate(reanimateTemplateEffect);

            IllusoryDecoy illusoryDecoyTemplateEffect = new IllusoryDecoy();
            effectBroker.RegisterEffectTemplate(illusoryDecoyTemplateEffect);

            //The 'Blind' spell effect is needed for dirty tricks to work
            Blind blindTemplateEffect = new Blind();
            effectBroker.RegisterEffectTemplate(blindTemplateEffect);

            WindWalk windWalkTemplateEffect = new WindWalk();
            effectBroker.RegisterEffectTemplate(windWalkTemplateEffect);

            Seeking effect = new Seeking();
            effect.SetCustomName(Text.SeekingPotionName.Get());
            effectBroker.RegisterEffectTemplate(effect, true);
            PotionRecipe recipe = effectBroker.GetEffectPotionRecipe(effect);
            potionOfSeekingRecipeKey = recipe.GetHashCode();
        }


        /// <summary>
        /// Event handler triggered when creating a new character.
        /// </summary>
        private void StartGameBehaviour_OnStartGameHandler(object sender, EventArgs e)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            if (startGameWithPotionOfSeeking)
            {
                //when new character is created, add Potion Of Seeking recipe to their inventory
                DaggerfallUnityItem potionRecipe = new DaggerfallUnityItem(ItemGroups.MiscItems, 4) { PotionRecipeKey = potionOfSeekingRecipeKey };
                player.Items.AddItem(potionRecipe);

                //player must have nabbed a potion from a kiosk
                DaggerfallUnityItem item = ItemBuilder.CreatePotion(potionOfSeekingRecipeKey);
                player.Items.AddItem(item);
            }

            if (enableHerbalism)
            {
                //give the player starting herbalism equipment if high enough medical skill
                int medical = player.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
                if (medical >= 23)
                {
                    DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients2, MortarAndPestle.MortarAndPestleTemplateIndex);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.PlantIngredients2, (int)PlantIngredients2.Bamboo);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Rain_water);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_bulb);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Elixir_vitae);
                    player.Items.AddItem(item);
                }
            }
        }


        /// <summary>
        /// Event handler triggered when a game has started loading.
        /// </summary>
        private void SaveLoadManager_OnStartLoadHandler(SaveData_v1 saveData)
        {
            WindWalk.Reset();
        }


        /// <summary>
        /// Event handler triggered when a game has finished loading.
        /// </summary>
        private void SaveLoadManager_OnLoadHandler(SaveData_v1 saveData)
        {
            PenwickMinion.InitializeOnLoad();

            if (enableTrapping)
                Trapping.OnLoadEvent();

            if (enableHerbalism)
                Herbalism.OnLoadEvent();

        }


        /// <summary>
        /// Event handler triggered when player takes a long rest (6 hours or more?)
        /// Heal/recharge minions after lengthy rest, if possible.
        /// Distant following minions catch up to player.
        /// </summary>
        private void DaggerfallRestWindow_OnSleepEndHandler()
        {
            PenwickMinion.RepositionFollowers();
            PenwickMinion.Rest();
        }


        /// <summary>
        /// Event handler triggered when player dies
        /// </summary>
        private void PlayerDeath_OnPlayerDeathHandler(object sender, EventArgs e)
        {
            WindWalk.Reset();
        }


        /// <summary>
        /// Event handler triggered when new enemy spawns
        /// </summary>
        private void GameManager_OnEnemySpawnHandler(GameObject enemy)
        {
            Loot.AddItems(enemy);
        }


        /// <summary>
        /// Event handler triggered when player exits a building
        /// </summary>
        private void PlayerEnterExit_OnTransitionExteriorHandler(PlayerEnterExit.TransitionEventArgs args)
        {
            SetWindWalkExitingCountdown();
        }

        /// <summary>
        /// Event handler triggered when player exits a dungeon
        /// </summary>
        private void PlayerEnterExit_OnTransitionDungeonExteriorHandler(PlayerEnterExit.TransitionEventArgs args)
        {
            SetWindWalkExitingCountdown();
        }


        /// <summary>
        /// It takes a bit of time to stabilize player location when exiting a location.
        /// Inform any active WindWalk effects.
        /// </summary>
        private void SetWindWalkExitingCountdown()
        {
            IEntityEffect effect = GameManager.Instance.PlayerEffectManager.FindIncumbentEffect<WindWalk>();
            if (effect == null)
                return;

            WindWalk windWalkEffect = effect as WindWalk;
            windWalkEffect.SetExitLocationCountdown();
        }


    } //class ThePenwickPapersMod



} //namespace