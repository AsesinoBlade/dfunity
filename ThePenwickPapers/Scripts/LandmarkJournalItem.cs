// Project:   Landmark Journal for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date: Apr 2021

using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect;
using DaggerfallWorkshop;

namespace ThePenwickPapers
{
    public class LandmarkJournalItem : DaggerfallUnityItem
    {
        public const int LandmarkJournalTemplateIndex = 1770;

        private const int baseValue = 65;    // Base gold value


        public LandmarkJournalItem() : this(baseValue)
        {
        }


        public LandmarkJournalItem(int baseValue) : base(ItemGroups.UselessItems2, LandmarkJournalTemplateIndex)
        {
            value = baseValue;

            //subscribe event handler to clear dungeon locations when entering/exiting a dungeon
            PlayerEnterExit.OnTransitionDungeonInterior += HandleDungeonTransition;
            PlayerEnterExit.OnTransitionDungeonExterior += HandleDungeonTransition;
        }


        void HandleDungeonTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            ThePenwickPapersMod.Persistent.DungeonLocations.Clear();
        }


        public override bool UseItem(ItemCollection collection)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            if (GameManager.Instance.AreEnemiesNearby(true, false))
            {
                DaggerfallUI.MessageBox(Text.EnemiesNear.Get());
            }
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
            {
                DaggerfallUI.MessageBox(Text.NotWhileSubmerged.Get());
            }
            else if (!GameManager.Instance.PlayerMotor.IsGrounded)
            {
                DaggerfallUI.MessageBox(Text.NotGrounded.Get());
            }
            else if (playerEntity.CarriedWeight > playerEntity.MaxEncumbrance)
            {
                DaggerfallUI.MessageBox(Text.YouAreEncumbered.Get());
            }
            else if ((playerEntity.CurrentFatigue / DaggerfallEntity.FatigueMultiplier) < 10)
            {
                DaggerfallUI.MessageBox(Text.TooTired.Get());
            }
            else if (playerEntity.IsParalyzed)
            {
                DaggerfallUI.MessageBox(Text.YouAreParalyzed.Get());
            }
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                if (AreTeleportersNearby())
                {
                    DaggerfallUI.MessageBox(Text.TeleportersNearby.Get());
                }
                else
                {
                    ShowLandmarkJournalDialog(ThePenwickPapersMod.Persistent.DungeonLocations);
                }
            }
            else if (GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
            {
                List<LandmarkLocation> locations;
                string key = GetLocationIdentifier();

                if (!ThePenwickPapersMod.Persistent.Towns.TryGetValue(key, out locations))
                {
                    locations = new List<LandmarkLocation>();
                    ThePenwickPapersMod.Persistent.Towns.Add(key, locations);
                }

                locations.Sort(); //town locations are sorted alphabetically
                ShowLandmarkJournalDialog(locations);
            }
            else
            {
                DaggerfallUI.MessageBox(Text.NotInDungeonOrTown.Get());
            }


            return true;
        }


        public override bool IsStackable()
        {
            return false;
        }



        public override bool IsEnchanted
        {
            get { return false; }
        }


        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(LandmarkJournalItem).ToString();
            return data;
        }


        //check if dungeon teleporter exits are nearby
        private bool AreTeleportersNearby()
        {
            Dictionary<string, Automap.AutomapDungeonState> automapState = Automap.instance.GetState();

            string locationIdentifier = GetLocationIdentifier();

            Automap.AutomapDungeonState dungeonState;

            //get state of this specific dungeon
            if (automapState.TryGetValue(locationIdentifier, out dungeonState))
            {
                Dictionary<string, Automap.TeleporterConnection> teleporters = dungeonState.dictTeleporterConnections;

                const float maxProximity = 40;

                foreach (KeyValuePair<string, Automap.TeleporterConnection> kvp in teleporters)
                {
                    if (GameManager.Instance.PlayerMotor.DistanceToPlayer(kvp.Value.teleporterExit.position) < maxProximity)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void ShowLandmarkJournalDialog(List<LandmarkLocation> locations)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            DaggerfallUI.Instance.PlayOneShot(SoundClips.PageTurn);

            LandmarkJournalPopupWindow landmarkJournalDialog = new LandmarkJournalPopupWindow(DaggerfallUI.UIManager, locations);
            DaggerfallUI.Instance.UserInterfaceManager.PushWindow(landmarkJournalDialog);
        }

        private string GetLocationIdentifier()
        {
            DFLocation dfLocation = GameManager.Instance.PlayerGPS.CurrentLocation;
            return string.Format("{0}/{1}", dfLocation.RegionName, dfLocation.Name);
        }


    }
}

