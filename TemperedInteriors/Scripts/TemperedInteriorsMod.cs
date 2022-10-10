// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility;

namespace TemperedInteriors
{
    public class TemperedInteriorsMod : MonoBehaviour
    {
        static Mod mod;
        static DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

        public static TemperedInteriorsMod Instance;

        public PlayerEnterExit.TransitionEventArgs TransitionArgs;
        public bool UsingHiResTextures;
        public byte Quality;
        public DFLocation.BuildingTypes BuildingType;
        public FactionFile.FactionIDs Faction;
        public Vector3 ProprietorLocation;
        public ClimateBases ClimateBase;
        public List<GameObject> Doors;

        //??????????????
        float lastTriggerTime;
        TownUtility townUtility;



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
            if (Physics.Raycast(flat.transform.position, Vector3.down, out RaycastHit hitInfo, 20f) == false)
                return false;

            //Check with flat position near eye level
            Vector3 eyeLevelPos = hitInfo.point + (Vector3.up * 2);

            Vector3 proprietorEyePos = ProprietorLocation + Vector3.up;

            Vector3 direction = eyeLevelPos - proprietorEyePos;

            float range = Mathf.Min(direction.magnitude, 30f);

            Ray ray = new Ray(proprietorEyePos, direction.normalized);
            bool hit = Physics.Raycast(ray, out hitInfo, range, 1);

            return hit == false;
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

            Debug.Log("Finished Start(): TemperedInteriors");
        }



        void Update()
        {
            if (TransitionArgs != null)
            {
                try
                {
                    AdjustInterior();
                }
                finally
                {
                    TransitionArgs = null;
                }
            }

            //ShowInfo();
        }


        /// <summary>
        /// Info gathering code, for debugging and testing
        /// </summary>
        void ShowInfo()
        {
            if (townUtility == null)
                townUtility = new TownUtility();


            if (InputManager.Instance.ActionStarted(InputManager.Actions.SwingWeapon))
            {
                Camera camera = GameManager.Instance.MainCamera;
                int playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                float maxDistance = 16;

                if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, playerLayerMask))
                {
                    float sizeY = hitInfo.collider.bounds.size.y;
                    Debug.Log("hitY=" + hitInfo.point.y + " objY=" + hitInfo.collider.transform.position.y + "  objHeight=" + sizeY + "  obj=" + hitInfo.collider.gameObject + "  " + Time.time);
                }
            }


            if (GameManager.Instance.PlayerGPS.IsPlayerInTown())
            {
                if (Time.time > lastTriggerTime + 2)
                {
                    lastTriggerTime = Time.time;

                    if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
                        Debug.Log("Location: " + GameManager.Instance.PlayerObject.transform.position);
                    else
                        townUtility.ShowInfo();
                }
            }
        }


        /// <summary>
        /// Event handler triggered when player enters a building.
        /// </summary>
        void PlayerEnterExit_OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            //AdjustInterior() will be called on next Update() after transition is fully complete
            TransitionArgs = args;
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

            Doors = new List<GameObject>();

            Quality = TransitionArgs.DaggerfallInterior.BuildingData.Quality;
            BuildingType = TransitionArgs.DaggerfallInterior.BuildingData.BuildingType;
            Faction = (FactionFile.FactionIDs)GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.factionID;

            //DaggerfallUI.AddHUDText(BuildingType.ToString());
            //DaggerfallUI.AddHUDText("Building quality " + Quality);

            GameManager game = GameManager.Instance;

            ClimateBase = ClimateBases.Temperate;
            if (game.PlayerEnterExit.OverrideLocation)
                ClimateBase = game.PlayerEnterExit.OverrideLocation.Summary.Climate;
            else
                ClimateBase = ClimateSwaps.FromAPIClimateBase(game.PlayerGPS.ClimateSettings.ClimateType);


            ProprietorLocation = FindProprietor();

            AdjustModels();

            AdjustFlats();

            Filth.Add();

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



        static readonly List<uint> chairs = new List<uint>() { 41100, 41101, 41103, 41119, 41102, 41122, 41123 };
        static readonly List<uint> tables = new List<uint>() { 41130, 41121, 41112, 51103, 41108, 51104, 41109, 41110 };

        /// <summary>
        /// Adjustments for 3D models.
        /// Models may have their textures changed to match quality, or removed altogether.
        /// </summary>
        void AdjustModels()
        {
            Regex rgx = new Regex(@"(\d+)");

            DaggerfallMesh[] models = FindObjectsOfType<DaggerfallMesh>();

            foreach (DaggerfallMesh model in models)
            {
                if (model.GetComponent<QuestResourceBehaviour>())
                    continue; //skip quest objects

                Match match = rgx.Match(model.name);
                if (match.Groups.Count != 2)
                    continue;

                uint modelID = uint.Parse(match.Groups[1].Value);

                if (modelID >= 41000 && modelID <= 41002) //beds
                {
                    ChangeBedTextures(model.gameObject);
                }
                else if (chairs.Contains(modelID)) //chairs
                {
                    bool throne = modelID == 41102 || modelID == 41122 || modelID == 41123;
                    ChangeChairTextures(model.gameObject, throne);
                }
                else if (tables.Contains(modelID)) //tables
                {
                    ChangeTableTextures(model.gameObject);
                }
                else if (modelID == 41126 || modelID == 41105 || modelID == 41106)
                {
                    ChangeBenchTextures(model.gameObject);
                }
                else if (modelID >= 74800 && modelID <= 74808) //big carpets
                {
                    ChangeCarpetTextures(model.gameObject);
                }
                else if (modelID >= 75800 && modelID <= 75808) //small carpets
                {
                    ChangeCarpetTextures(model.gameObject);
                }
                else if (modelID >= 42500 && modelID <= 42535) //banners
                {
                    ChangeTapestryTextures(model.gameObject);
                }
                else if (modelID >= 42536 && modelID <= 42571) //tapestries
                {
                    ChangeTapestryTextures(model.gameObject);
                }
                else if (modelID >= 51115 && modelID <= 51120) //paintings
                {
                    ModifyPainting(model.gameObject);
                }
                else if (modelID == 41120 && Quality < 7) //organ
                {
                    GameObject.Destroy(model.gameObject);
                }
                else if (modelID == 9000) //interior door
                {
                    Doors.Add(model.gameObject);
                    ChangeDoorTexture(model.gameObject);
                }
                else
                {
                    continue;
                }

            }
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
        void ChangeTableTextures(GameObject table)
        {
            (int, int) texture;

            bool scaleDown = !UsingHiResTextures;

            if (Quality > 16)
            {
                texture = (67, 2);
                scaleDown = false;
            }
            else if (Quality > 11)
                texture = (366, 4);
            else if (Quality < 5)
                texture = (321, 2); //texture = (171, 0);
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
        /// Changing (interior) door textures to match building quality.
        /// </summary>
        void ChangeDoorTexture(GameObject door)
        {
            bool scaleDown = !UsingHiResTextures;

            if (Quality > 13)
            {
                Utility.SwapModelTexture(door, (374, 0), Textures.DoorHi, scaleDown);
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