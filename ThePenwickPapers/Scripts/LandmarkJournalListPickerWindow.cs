// Project:   Landmark Journal for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date: Apr 2021

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Items;
using DaggerfallConnect;

namespace ThePenwickPapers
{
    public class LandmarkJournalListPickerWindow : DaggerfallListPickerWindow
    {
        private readonly List<LandmarkLocation> locations;
        private PlayerEntity player;



        public LandmarkJournalListPickerWindow(List<LandmarkLocation> locations)
            : base(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow)
        {
            this.locations = locations;
            player = GameManager.Instance.PlayerEntity;
        }


        protected override void Setup()
        {
            base.Setup();

            OnItemPicked += LocationPicker_OnItemPicked;

            // Populate a list with remembered locations that the player can choose from.
            foreach (LandmarkLocation loc in locations)
            {
                ListBox.AddItem(loc.Name);
            }

            listBox.SelectNone();

        }


        /// <summary>
        /// Triggered when an item is chosed from the landmark location list window.
        /// </summary>
        public void LocationPicker_OnItemPicked(int index, string name)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            Vector3 destination = locations[index].Position;
            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            float distance = playerMotor.DistanceToPlayer(destination);

            bool isInside = GameManager.Instance.PlayerEnterExit.IsPlayerInside;
            
            bool isRiding = false;

            if (distance > 5)
            {
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();

                playerMotor.transform.position = destination;
                playerMotor.FixStanding();

                //reposition any following minions nearby if possible
                PenwickMinion.RepositionFollowers();

                AudioSource audioSource = DaggerfallUI.Instance.AudioSource;

                if (distance > 60 && !isInside && GameManager.Instance.TransportManager.HasHorse())
                {
                    AudioClip clip = DaggerfallUI.Instance.DaggerfallAudioSource.GetAudioClip((int)SoundClips.HorseClop);
                    audioSource.PlayOneShot(clip);
                    Thread.Sleep(1400);
                    audioSource.Stop();

                    isRiding = true;
                }
                else if (distance > 60 && !isInside && GameManager.Instance.TransportManager.HasCart())
                {
                    AudioClip clip = DaggerfallUI.Instance.DaggerfallAudioSource.GetAudioClip((int)SoundClips.HorseAndCart);
                    audioSource.PlayOneShot(clip);
                    Thread.Sleep(1400);
                    audioSource.Stop();

                    isRiding = true;
                }
                else
                {
                    SoundClips clip = GetFootstepSound();
                    Thread.Sleep(200);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                }

                try
                {
                    HandleTravelCost(distance, isRiding);
                }
                catch (System.Exception e)
                {
                    Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                    Debug.LogException(e);
                }

                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack(3.0f);
            }
        }



        /// <summary>
        /// Handles fatigue loss, time loss, and checks for any incident that occurs during travel.
        /// </summary>
        private bool HandleTravelCost(float distance, bool isRiding)
        {
            bool isInside = GameManager.Instance.PlayerEnterExit.IsPlayerInside;

            if (!isInside)
            {
                int streetwise = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);

                //lower streetwise results in longer trip
                float streetwiseMod = (100 - streetwise) / 100;
                distance += distance * streetwiseMod;
            }
            else
            {
                distance *= 4; //dungeons tend to be tangled
            }

            float encumbranceModifier = 1;

            if (!isRiding)
                encumbranceModifier += player.CarriedWeight / player.MaxEncumbrance;

            //Calculate fatigue loss
            double fatigueLoss = distance * encumbranceModifier / 100;
            if (!isRiding && player.Career.Athleticism)
                fatigueLoss *= player.ImprovedAthleticism ? 0.6 : 0.8;

            player.DecreaseFatigue((int)fatigueLoss, true);


            //Calculate time loss
            float speedModifier = isRiding ? 3 : 400 / (player.Stats.LiveSpeed + 30);
            float travelTime = distance * speedModifier * encumbranceModifier;
            DaggerfallUnity.Instance.WorldTime.RaiseTimeInSeconds += travelTime;

            //check for any unfortunate incident during travel
            return isInside ? CheckForDungeonIncident(distance) : CheckForTownIncident(distance);
        }


        /// <summary>
        /// Checks if PC was exposed to any disease-causing spores while traveling in a dungeon.
        /// </summary>
        private bool CheckForDungeonIncident(float distance)
        {
            bool hadIncident = false;

            float sporeChance = (130 - Mathf.Clamp(player.Stats.LiveLuck, 0, 100)) / 50;
            sporeChance *= distance / 100;
            sporeChance = Mathf.Clamp(sporeChance, 0.1f, 10f);

            if (player.Level < 5)
            {
                //go easy on lower level characters
            }
            else if (Random.Range(0.0f, 100.0f) < sporeChance)
            {
                hadIncident = true;

                Diseases[] diseases = new Diseases[] {
                    Diseases.Consumption, Diseases.Cholera, Diseases.TyphoidFever
                };

                FormulaHelper.InflictDisease(player, player, diseases);
                DaggerfallUI.MessageBox(Text.InhaledSpores.Get());

            }

            return hadIncident;
        }


        /// <summary>
        /// Checks for thugs, pickpockets, and any plague-carriers while PC is traveling in town.
        /// </summary>
        private bool CheckForTownIncident(float distance)
        {
            bool hadIncident;

            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            bool[] regionFlags = player.RegionData[regionIndex].Flags;

            if (DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight)
            {
                hadIncident = CheckForThugs(distance, regionFlags);
            }
            else
            {
                hadIncident = CheckForPlague(distance, regionFlags) || CheckForCutpurse(distance, regionFlags);
            }

            return hadIncident;
        }


        /// <summary>
        /// Check if PC had an encounter with thugs while traveling in town at night.
        /// </summary>
        private bool CheckForThugs(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool crimeWave = regionFlags[(int)PlayerEntity.RegionDataFlags.CrimeWave];

            float crimeChance = (distance / 100) * (crimeWave ? 18 : 8);

            if (Random.Range(0.0f, 100.0f) < crimeChance)
            {
                hadIncident = true;

                if (player.Career.AcuteHearing && Dice100.SuccessRoll(50))
                {
                    DaggerfallUI.MessageBox(Text.AmbushAverted.Get());
                }
                else
                {
                    DaggerfallUI.MessageBox(Text.Thugs.Get());
                    SpawnThugs(player.Level / 8 + 1);
                }
            }

            return hadIncident;
        }


        /// <summary>
        /// Spawns enemies to attack the PC.
        /// </summary>
        private void SpawnThugs(int count)
        {
            MobileTypes[] thiefTypes = { MobileTypes.Rogue, MobileTypes.Thief, MobileTypes.Burglar };
            MobileTypes[] fighterTypes = { MobileTypes.Archer, MobileTypes.Barbarian, MobileTypes.Warrior };

            MobileTypes[] thugTypes = Dice100.SuccessRoll(50) ? thiefTypes : fighterTypes;

            while (count-- > 0)
            {
                MobileTypes enemyType = thugTypes[Random.Range(0, thugTypes.Length)];

                GameObjectHelper.CreateFoeSpawner(true, enemyType, 1, 4.0f, 10.0f);
            }
        }


        /// <summary>
        /// Check if PC had an encounter with a plague victim while traveling in town during the day.
        /// This can only occur if there is a plague occuring in the current region.
        /// </summary>
        private bool CheckForPlague(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool plagueOngoing = regionFlags[(int)PlayerEntity.RegionDataFlags.PlagueOngoing];

            if (plagueOngoing)
            {
                float plagueChance = (130 - Mathf.Clamp(player.Stats.LiveLuck, 0, 100)) / 10;
                plagueChance *= distance / 100;

                if (Random.Range(0.0f, 100.0f) < plagueChance)
                {
                    hadIncident = true;

                    Diseases[] diseases = new Diseases[] { Diseases.Plague };

                    FormulaHelper.InflictDisease(player, player, diseases);

                    DaggerfallUI.MessageBox(Text.PasserbyAvoidsPlagueVictim.Get());
                    DaggerfallUI.MessageBox(Text.EncounteredPlagueVictim.Get());
                }

            }

            return hadIncident;
        }


        /// <summary>
        /// Check if PC had an encounter with a pickpocket while traveling in town during the day.
        /// There will be a higher incidence if there is a crime wave currently occuring in the region.
        /// </summary>
        private bool CheckForCutpurse(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool crimeWave = regionFlags[(int)PlayerEntity.RegionDataFlags.CrimeWave];
            float crimeChance = (distance / 100) * (crimeWave ? 18 : 8);

            if (Random.Range(0.0f, 100.0f) < crimeChance)
            {
                hadIncident = true;

                player.TallySkill(DFCareer.Skills.Streetwise, 1);

                int streetwise = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);
                int pickChance = player.Skills.GetLiveSkillValue(DFCareer.Skills.Pickpocket);

                if (Dice100.SuccessRoll(streetwise))
                {
                    //chance at simultaneously picking the cutpurse's pocket
                    if (Dice100.SuccessRoll(pickChance))
                    {
                        player.TallySkill(DFCareer.Skills.Pickpocket, 2);

                        string itemName;
                        DaggerfallUnityItem item = SnatchItem(out itemName);

                        if (item != null)
                        {
                            player.Items.AddItem(item);
                        }

                        if (HandleLostGold() > 0)
                        {
                            string msg = Text.LostGoldButSnatchedItem.Get(itemName); 
                            DaggerfallUI.MessageBox(msg);
                        }
                        else
                        {
                            string msg = Text.NabbedItem.Get(itemName); 
                            DaggerfallUI.MessageBox(msg);
                        }

                        //this message will be layered on top of the one above
                        DaggerfallUI.MessageBox(Text.SimultaneousPickpocket.Get()); 
                    }
                    else
                    {
                        DaggerfallUI.MessageBox(Text.CutpurseFails.Get());
                    }
                }
                else if (HandleLostGold() > 0)
                {
                    DaggerfallUI.MessageBox(Text.CutpurseSucceeds.Get());
                }
            }

            return hadIncident;
        }


        /// <summary>
        /// Handles loss of gold to a pickpocket.
        /// </summary>
        private int HandleLostGold()
        {
            int goldLost = player.GoldPieces / (Random.Range(5, 20));

            if (goldLost > 150)
            {
                //put a cap on how much a cutpurse can actually steal
                goldLost = Random.Range(110, 150);
            }

            player.DeductGoldAmount(goldLost);

            return goldLost;
        }


        /// <summary>
        /// During simultaneous pickpocket events, this determines what item the PC nabs.
        /// </summary>
        private DaggerfallUnityItem SnatchItem(out string itemName)
        {
            DaggerfallUnityItem item = null;

            switch (Random.Range(0, 7))
            {
                case 1:
                    item = ItemBuilder.CreateRandomGem();
                    itemName = Text.ItemGem.Get();
                    break;
                case 2:
                    item = ItemBuilder.CreateRandomJewellery();
                    itemName = Text.ItemJewellery.Get();
                    break;
                case 3:
                    item = ItemBuilder.CreateRandomDrug();
                    itemName = Text.ItemDrug.Get();
                    break;
                case 4:
                    item = ItemBuilder.CreateRandomPotion(1);
                    itemName = Text.ItemPotion.Get();
                    break;
                case 5:
                    item = ItemBuilder.CreateGoldPieces(Random.Range(10, 200));
                    itemName = Text.ItemGold.Get();
                    break;
                case 6:
                    item = ItemBuilder.CreateRandomBook();
                    itemName = Text.ItemBook.Get();
                    break;
                default:
                    itemName = Text.ItemPocketLint.Get();
                    break;
            }

            return item;
        }

        /// <summary>
        /// Determines what the appropriate footstep noise is based on the current terrain and season.
        /// </summary>
        private SoundClips GetFootstepSound()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                return SoundClips.PlayerFootstepStone1;

            bool isWinter = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue == DaggerfallDateTime.Seasons.Winter;
            bool canHaveSnow = !WeatherManager.IsSnowFreeClimate(GameManager.Instance.PlayerGPS.CurrentClimateIndex);

            if (GameManager.Instance.PlayerMotor.OnExteriorPath)
                return SoundClips.PlayerFootstepStone2;
            else if (isWinter && canHaveSnow)
                return SoundClips.PlayerFootstepSnow1;
            else
                return SoundClips.PlayerFootstepOutside1;
        }




    } //class LandmarkJournalListPickerWindow


} //namespace
