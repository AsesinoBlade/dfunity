// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility;
using System;

namespace TemperedInteriors
{
    public class TemperedInteriorsMod : MonoBehaviour
    {
        static Mod mod;

        public static TemperedInteriorsMod Instance;

        public PlayerEnterExit.TransitionEventArgs TransitionArgs;
        public bool UsingHiResTextures;
        public byte Quality;
        public DFLocation.BuildingTypes BuildingType;
        public FactionFile.FactionIDs Faction;
        public Vector3 ProprietorLocation;
        public ClimateBases ClimateBase;



        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<TemperedInteriorsMod>();

            mod.IsReady = true;
        }


        /// <summary>
        /// If object is visible to the proprietor, it is probably in or near the main room of
        /// the establishment.
        /// </summary>
        public bool IsVisibleToProprietor(DaggerfallBillboard flat)
        {
            Vector3 groundPos = Utility.FindGround(flat.transform.position);

            //Check with flat position near eye level
            Vector3 eyeLevelPos = groundPos + (Vector3.up * 2);

            Vector3 proprietorEyePos = ProprietorLocation + Vector3.up;

            Vector3 direction = eyeLevelPos - proprietorEyePos;

            float range = Mathf.Min(direction.magnitude, 30f);

            Ray ray = new Ray(proprietorEyePos, direction.normalized);
            bool blocked = Physics.Raycast(ray, range, 1);

            return blocked == false;
        }




        void Start()
        {
            Debug.Log("Start(): TemperedInteriors");

            Instance = this;

            //event handler registration
            PlayerEnterExit.OnTransitionInterior += PlayerEnterExit_OnTransitionInterior;

            //prevent building interior models from being combined
            DaggerfallUnity.Instance.Option_CombineRMB = false;

            TexturesExtension.Load();

            UsingHiResTextures = ModManager.Instance.GetMod("DREAM - TEXTURES") != null;

            Lighting.Init();

            Debug.Log("Finished Start(): TemperedInteriors");
        }



        void Update()
        {
            if (GameManager.IsGamePaused)
                return;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
                Lighting.AdjustAmbientLight();
        }


        /// <summary>
        /// Event handler triggered when player enters a building.
        /// </summary>
        void PlayerEnterExit_OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            TransitionArgs = args;

            try
            {
                Lighting.SetGroundLevel(TransitionArgs);

                AdjustInterior();
            }
            catch (Exception e)
            {
                Debug.LogError("Exception in TemperedInteriorsMod: " + e.ToString());
            }
        }


        /// <summary>
        /// Entry point for making interior adjustments.
        /// </summary>
        void AdjustInterior()
        {
            //Seeded random number generator to keep random values for the building consistent through the day
            int seed = (int)Utility.GenerateHashValue(TransitionArgs.DaggerfallInterior, Vector3.zero);
            seed += DaggerfallUnity.Instance.WorldTime.Now.DayOfYear;
            Utility.Init(seed);

            Quality = TransitionArgs.DaggerfallInterior.BuildingData.Quality;
            BuildingType = TransitionArgs.DaggerfallInterior.BuildingData.BuildingType;
            Faction = (FactionFile.FactionIDs)GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.factionID;

            GameManager game = GameManager.Instance;

            ClimateBase = ClimateBases.Temperate;
            if (game.PlayerEnterExit.OverrideLocation)
                ClimateBase = game.PlayerEnterExit.OverrideLocation.Summary.Climate;
            else
                ClimateBase = ClimateSwaps.FromAPIClimateBase(game.PlayerGPS.ClimateSettings.ClimateType);


            ProprietorLocation = FindProprietor();


            List<GameObject> doors = GetDoors();

            CheckReverseDoorHinges(doors);

            foreach (GameObject door in doors)
                ChangeDoorTexture(door);


            AdjustModels();

            AdjustFlats();

            Filth.Add(doors);
        }


        /// <summary>
        /// Locate the building proprietor (merchant) if this is a store or tavern.
        /// </summary>
        Vector3 FindProprietor()
        {
            StaticNPC[] npcs = FindObjectsOfType<StaticNPC>();

            foreach (StaticNPC npc in npcs)
            {
                if (npc.Data.factionID == (int)FactionFile.FactionIDs.The_Merchants)
                {
                    return npc.transform.position;
                }
            }

            return Vector3.zero;
        }



        /// <summary>
        /// Returns list of interior action doors (model 9000).
        /// </summary>
        List<GameObject> GetDoors()
        {
            Regex rgx = new Regex(@"ID=(\d+)");

            List<GameObject> doors = new List<GameObject>();

            GameObject[] models = GetChildObjects("Action Doors");

            foreach (GameObject model in models)
            {
                Match match = rgx.Match(model.name);
                if (match.Groups.Count != 2)
                    continue;

                uint modelID = uint.Parse(match.Groups[1].Value);
                if (modelID == 9000) //interior door
                    doors.Add(model);
            }

            return doors;
        }


        /// <summary>
        /// If door is blocked from opening by furniture or a wall, then reverse the hinges.
        /// This needs to be done before the door's Start() method is called to initialize the door correctly.
        /// </summary>
        void CheckReverseDoorHinges(List<GameObject> doors)
        {
            foreach (GameObject door in doors)
            {
                Renderer renderer = door.GetComponent<MeshRenderer>();

                //Doing raycasts from lower-center of door
                Vector3 pos = renderer.bounds.center;
                pos += Vector3.down * (renderer.bounds.extents.y - 0.2f);
                if (Physics.Raycast(pos, door.transform.forward, 1.2f))
                {
                    //Blocked in forward direction, check if NOT blocked in reverse direction
                    if (!Physics.Raycast(pos, -door.transform.forward, 1.2f))
                    {
                        //Rotate door 180 degrees around y-axis, centered in middle of model
                        door.transform.RotateAround(renderer.bounds.center, Vector3.up, 180);
                    }
                }

            }
        }



        static readonly List<uint> chairs = new List<uint>() { 41100, 41101, 41103, 41119, 41102, 41122, 41123 };
        static readonly List<uint> tables = new List<uint>() { 41130, 41121, 41112, 51103, 41108, 51104, 41109, 41110 };

        /// <summary>
        /// Adjustments for 3D models.
        /// Models may have their textures changed to match quality, or removed altogether.
        /// </summary>
        void AdjustModels()
        {
            Regex rgx = new Regex(@"ID=(\d+)");

            GameObject[] models = GetChildObjects("Models");

            foreach (GameObject model in models)
            {
                if (model.GetComponent<QuestResourceBehaviour>())
                    continue; //skip quest objects

                Match match = rgx.Match(model.name);
                if (match.Groups.Count != 2)
                    continue;

                uint modelID = uint.Parse(match.Groups[1].Value);

                if (modelID >= 41000 && modelID <= 41002) //beds
                {
                    ChangeBedTextures(model);
                }
                else if (chairs.Contains(modelID)) //chairs
                {
                    bool throne = modelID == 41102 || modelID == 41122 || modelID == 41123;
                    ChangeChairTextures(model, throne);
                }
                else if (tables.Contains(modelID)) //tables
                {
                    ChangeTableTextures(model, modelID);
                }
                else if (modelID == 41126 || modelID == 41105 || modelID == 41106) //benches
                {
                    ChangeBenchTextures(model);
                }
                else if (modelID == 41800 || modelID == 41801 || modelID == 41003 || modelID == 41004) //wardrobes
                {
                    ChangeWardrobeTextures(model);
                }
                else if (modelID >= 74800 && modelID <= 74808) //big carpets
                {
                    ChangeCarpetTextures(model);
                }
                else if (modelID >= 75800 && modelID <= 75808) //small carpets
                {
                    ChangeCarpetTextures(model);
                }
                else if (modelID >= 42500 && modelID <= 42535) //banners
                {
                    ChangeTapestryTextures(model);
                }
                else if (modelID >= 42536 && modelID <= 42571) //tapestries
                {
                    ChangeTapestryTextures(model);
                }
                else if (modelID >= 51115 && modelID <= 51120) //paintings
                {
                    ModifyPainting(model);
                }
                else if (modelID == 41120 && Quality < 7) //organ
                {
                    GameObject.Destroy(model);
                }
                else
                {
                    continue;
                }

            }


        }


        /// <summary>
        /// Returns child GameObjects that are parented to the named object.
        /// </summary>
        GameObject[] GetChildObjects(string parentName)
        {
            Transform root = TransitionArgs.DaggerfallInterior.transform.Find(parentName);
            if (root == null)
                return new GameObject[0];

            GameObject[] childObjects = new GameObject[root.childCount];
            for (int i = 0; i < root.childCount; ++i)
            {
                childObjects[i] = root.GetChild(i).gameObject;
            }

            return childObjects;
        }


        /// <summary>
        /// Changing bed texture to match building quality.  Possibly add stains to bed.
        /// </summary>
        void ChangeBedTextures(GameObject bed)
        {
            if (Quality < 6)
            {
                Utility.SwapModelTexture(bed, (90, 5), Textures.BedTopLow);
                Utility.SwapModelTexture(bed, (90, 6), Textures.BedSideLow);
                Utility.SwapModelTexture(bed, (90, 7), Textures.BedEndLow);
            }
            else if (Quality > 15)
            {
                Utility.SwapModelTexture(bed, (90, 5), Textures.BedTopHi);
                Utility.SwapModelTexture(bed, (90, 6), Textures.BedSideHi);
                Utility.SwapModelTexture(bed, (90, 7), Textures.BedEndHi);
            }

            Vector3 center = Utility.FindGround(bed.transform.position + Vector3.up * 0.3f);

            //Potentially add some stains
            while (Quality < Utility.Random(-4, 11))
            {
                Vector3 pos = center;
                pos += bed.transform.right * Utility.Random(-0.4f, 0.4f);
                pos += bed.transform.forward * Utility.Random(-0.4f, 0.4f);
                Filth.AddStain(pos, Vector3.up, bed.transform.parent);
            }

        }


        /// <summary>
        /// Changing chair texture to match building quality.
        /// </summary>
        void ChangeChairTextures(GameObject chair, bool throne)
        {
            (int, int) texture;

            bool scaleDown = !UsingHiResTextures;

            if (Quality > 18 && throne)
                texture = (450, 6);
            else if (Quality > 16)
                texture = (87, 8);
            else if (Quality > 13)
                texture = (446, 1);
            else if (Quality > 9)
                texture = (50, 0);
            else if (Quality < 5)
                texture = (321, 2);
            else
                return;

            Utility.SwapModelTexture(chair, (67, 0), texture, scaleDown);
        }


        /// <summary>
        /// Changing table texture to match building quality.
        /// </summary>
        void ChangeTableTextures(GameObject table, uint modelID)
        {
            (int, int) texture;

            bool scaleDown = !UsingHiResTextures;

            if (Quality > 16)
            {
                texture = (67, 2);
                scaleDown = false;
            }
            else if (Quality > 11 && modelID != 41121)
                texture = (366, 4);
            else if (Quality < 5)
                texture = (321, 2);
            else
                return;

            //different tables can have different textures, we'll check both primary possibilities
            Utility.SwapModelTexture(table, (67, 0), texture, scaleDown);
            Utility.SwapModelTexture(table, (67, 1), texture, scaleDown);
        }


        /// <summary>
        /// Changing bench texture to match building quality.
        /// </summary>
        void ChangeBenchTextures(GameObject bench)
        {
            (int, int) texture;

            bool scaleDown = !UsingHiResTextures;

            if (Quality > 16)
            {
                texture = (67, 2);
                scaleDown = false;
            }
            else if (Quality < 5)
                texture = (321, 2);
            else
                return;

            Utility.SwapModelTexture(bench, (67, 0), texture, scaleDown);
        }


        /// <summary>
        /// Changing wardrobe texture to match building quality.
        /// </summary>
        void ChangeWardrobeTextures(GameObject wardrobe)
        {
            if (Quality > 10)
            {
                Utility.SwapModelTexture(wardrobe, (90, 8), Textures.WardrobeFrontHi);
                Utility.SwapModelTexture(wardrobe, (90, 9), Textures.WardrobeFrontEdgeHi);
                Utility.SwapModelTexture(wardrobe, (90, 10), Textures.WardrobeSideEdgeHi);
                Utility.SwapModelTexture(wardrobe, (90, 11), Textures.WardrobeSideHi);
            }
        }


        /// <summary>
        /// Changing (interior) door textures to match building quality.
        /// </summary>
        void ChangeDoorTexture(GameObject door)
        {
            bool scaleDown = !UsingHiResTextures;

            if (Quality > 13)
            {
                //Current door texture depends on climate.
                //Attempting to check for and replace all likely values.
                Utility.SwapModelTexture(door, (74, 0), Textures.DoorHi, scaleDown);
                Utility.SwapModelTexture(door, (174, 0), Textures.DoorHi, scaleDown);
                Utility.SwapModelTexture(door, (374, 0), Textures.DoorHi, scaleDown);
                Utility.SwapModelTexture(door, (474, 0), Textures.DoorHi, scaleDown);

                //try to swap door edge texture as well
                Utility.SwapModelTexture(door, (0, 74), (0, 45));
                Utility.SwapModelTexture(door, (67, 12), (67, 10));
            }
        }


        /// <summary>
        /// Changing carpet textures to match building quality.  Possibly add stains.
        /// </summary>
        void ChangeCarpetTextures(GameObject carpet)
        {
            uint hash = Utility.GenerateHashValue(TransitionArgs.DaggerfallInterior, carpet.transform.position);

            bool scaleDown = !UsingHiResTextures;

            (int, int) textureValue =  Utility.ExtractMainTextureValue(carpet.GetComponent<MeshRenderer>());

            if (Quality < 6 || Quality < hash % 13)
            {
                Utility.SwapModelTexture(carpet, textureValue, Textures.CarpetLow, scaleDown);
                Utility.SwapModelTexture(carpet, (49, 9), Textures.CarpetEdgeLow1, scaleDown);
                Utility.SwapModelTexture(carpet, (49, 10), Textures.CarpetEdgeLow2, scaleDown);
            }

            if ((carpet.transform.forward - Vector3.up).magnitude > 0.01f)
                return; //not laying flat on floor

            Vector3 center = carpet.transform.position + Vector3.up * 0.11f;

            //Add some carpet stains
            while (Quality < Utility.Random(-3, 9))
            {
                Vector3 pos = center;
                pos += carpet.transform.right * Utility.Random(-1.5f, 1.5f);
                pos += carpet.transform.up * Utility.Random(-1.5f, 1.5f);
                Filth.AddStain(pos, Vector3.up, carpet.transform.parent);
            }

            if (Quality < hash % 8)
            {
                //Destroy carpet but keep stains
                GameObject.Destroy(carpet);
            }
        }


        /// <summary>
        /// Changing tapestry/banner textures to match building quality.
        /// </summary>
        void ChangeTapestryTextures(GameObject banner)
        {
            bool scaleDown = !UsingHiResTextures;

            if (Quality < 5)
            {
                GameObject.Destroy(banner);
            }
            else if (Quality < 10)
            {
                //get the current texture of the tapestry/banner...
                (int, int) textureValue = Utility.ExtractMainTextureValue(banner.GetComponent<MeshRenderer>());

                Utility.SwapModelTexture(banner, textureValue, Textures.TapestryLow, scaleDown);
            }
        }


        /// <summary>
        /// Possibly remove or skew paintings based on building quality.
        /// </summary>
        void ModifyPainting(GameObject painting)
        {
            uint hash = Utility.GenerateHashValue(TransitionArgs.DaggerfallInterior, painting.transform.position);

            if (Quality < hash % 9)
            {
                GameObject.Destroy(painting);
            }
            else if (hash % (Quality + 2) == 0 && painting.transform.forward.y == 0)
            {
                float rotation = hash % 16 - 8;
                painting.transform.Rotate(painting.transform.forward, rotation, Space.World);
            }
        }


        /// <summary>
        /// Make adjustments to 2D flats such as goblets, plants, lights, etc.
        /// </summary>
        void AdjustFlats()
        {
            DaggerfallBillboard[] flats = FindObjectsOfType<DaggerfallBillboard>();

            foreach (DaggerfallBillboard flat in flats)
            {
                if (flat.GetComponent<QuestResourceBehaviour>())
                    continue; //skipping quest objects

                if (flat.Summary.Archive == 200 && flat.Summary.Record <= 6) //goblets
                    SwapGoblet(flat);
                else if (flat.Summary.Archive == 210) //lighting
                    Lighting.Swap(flat, Quality);
                else if (flat.Summary.Archive == 211 && flat.Summary.Record == 40 && Quality < 9) //meat
                    Utility.SwapFlat(flat, (211, 10), Alignment.Ground);
                else if (flat.Summary.Archive == 213) //plants
                    AdjustPlant(flat);
                else if (flat.Summary.Archive == 254 && (flat.Summary.Record >= 26 && flat.Summary.Record <= 33))
                    AdjustPlant(flat);
                else
                    continue;
            }
        }


        /// <summary>
        /// Replace goblets with quality appropriate versions.
        /// </summary>
        void SwapGoblet(DaggerfallBillboard flat)
        {
            int record;

            if (Quality < 6)
                record = 6;
            else if (Quality < 12)
                record = Dice100.SuccessRoll(50) ? 0 : 2;
            else if (Quality < 17)
                record = Dice100.SuccessRoll(50) ? 1 : 3;
            else
                record = Dice100.SuccessRoll(50) ? 4 : 5;

            Utility.SwapFlat(flat, (200, record), Alignment.Ground);
        }


        /// <summary>
        /// Possibly remove some plants, depending on building quality.
        /// </summary>
        void AdjustPlant(DaggerfallBillboard flat)
        {
            uint hash = Utility.GenerateHashValue(TransitionArgs.DaggerfallInterior, flat.transform.position);

            if (BuildingType != DFLocation.BuildingTypes.Temple && Quality < hash % 10)
                GameObject.Destroy(flat.gameObject);

        }


    } //class TemperedInteriorsMod



} //namespace