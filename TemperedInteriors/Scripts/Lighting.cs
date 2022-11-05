// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;

namespace TemperedInteriors
{
    public static class Lighting
    {
        static PlayerAmbientLight playerAmbientLight;
        static Color defaultNightAmbient;
        static Color defaultDayAmbient;
        static Vector3 groundLevelPosition;


        /// <summary>
        /// Record the initial default interior ambient light colors.
        /// </summary>
        public static void Init()
        {
            playerAmbientLight = GameManager.Instance.PlayerObject.GetComponent<PlayerAmbientLight>();

            if (DaggerfallUnity.Settings.AmbientLitInteriors)
            {
                defaultNightAmbient = playerAmbientLight.InteriorNightAmbientLight_AmbientOnly;
                defaultDayAmbient = playerAmbientLight.InteriorAmbientLight_AmbientOnly;
            }
            else
            {
                defaultNightAmbient = playerAmbientLight.InteriorNightAmbientLight;
                defaultDayAmbient = playerAmbientLight.InteriorAmbientLight;
            }
        }


        /// <summary>
        /// Try to determine where 'ground level' is by looking for the lowest entry door.
        /// </summary>
        public static void SetGroundLevel(PlayerEnterExit.TransitionEventArgs args)
        {
            if (!args.DaggerfallInterior.FindLowestOuterInteriorDoor(out Vector3 pos, out _))
                pos = GameManager.Instance.PlayerObject.transform.position;

            groundLevelPosition = pos + Vector3.down; //close to floor
        }


        /// <summary>
        /// Adjust interior ambient light level by depth below ground.
        /// </summary>
        public static void AdjustAmbientLight()
        {
            float depth = GameManager.Instance.PlayerObject.transform.position.y - groundLevelPosition.y + 0.2f;

            depth = Mathf.Clamp(depth, -10f, 0f);

            float step = 1 + (depth / -3f);

            float scaler = 1f / step;

            Color ambientLightColor;
            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
                ambientLightColor = defaultNightAmbient * scaler;
            else
                ambientLightColor = defaultDayAmbient * scaler;

            SetAmbientLight(ambientLightColor);
        }


        /// <summary>
        /// Sets the ambient light color values of the player object PlayerAmbientLight component.
        /// </summary>
        static void SetAmbientLight(Color ambientLightColor)
        {
            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (DaggerfallUnity.Settings.AmbientLitInteriors)
                    playerAmbientLight.InteriorNightAmbientLight_AmbientOnly = ambientLightColor;
                else
                    playerAmbientLight.InteriorNightAmbientLight = ambientLightColor;
            }
            else
            {
                if (DaggerfallUnity.Settings.AmbientLitInteriors)
                    playerAmbientLight.InteriorAmbientLight_AmbientOnly = ambientLightColor;
                else
                    playerAmbientLight.InteriorAmbientLight = ambientLightColor;
            }

        }


        /// <summary>
        /// Potentially swap light with another to match building quality
        /// </summary>
        public static void Swap(DaggerfallBillboard flat, byte quality)
        {
            DFLocation.BuildingTypes buildingType = TemperedInteriorsMod.Instance.TransitionArgs.DaggerfallInterior.BuildingData.BuildingType;

            switch (flat.Summary.Record)
            {
                case 3:
                    if (quality > 16)
                        SwapLight(flat, 5, Alignment.Ground);
                    else if (quality > 7)
                        SwapLight(flat, quality < 16 ? 4 : 5, Alignment.Bottom); //candle-dish or candelabra
                    break;
                case 4:
                    if (quality < 8)
                        SwapLight(flat, 3, Alignment.Ground);
                    else if (quality > 16)
                        SwapLight(flat, 5, Alignment.Bottom);
                    break;
                case 5:
                    if (quality < 7)
                        SwapLight(flat, 3, Alignment.Ground);
                    else if (quality < 14)
                        SwapLight(flat, 4, Alignment.Ground);
                    break;
                case 8:
                    if (buildingType != DFLocation.BuildingTypes.GuildHall)
                    {
                        if (quality < 8)
                            SwapLight(flat, 25);
                        else if (quality < 14)
                            SwapLight(flat, 23, Alignment.Bottom);
                    }
                    break;
                case 9:
                    if (quality < 9)
                        SwapLight(flat, 25);
                    else if (quality < 14)
                        SwapLight(flat, 23);
                    break;
                case 11:
                    if (quality > 15)
                        SwapLight(flat, 13);
                    else if (quality > 11)
                        SwapLight(flat, 22);
                    else if (quality > 7)
                        SwapLight(flat, 26);
                    break;
                case 13:
                    if (buildingType != DFLocation.BuildingTypes.GuildHall)
                    {
                        if (quality < 7)
                            SwapLight(flat, 11);
                        else if (quality < 12)
                            SwapLight(flat, 27);
                        else if (quality < 16)
                            SwapLight(flat, 22);
                    }
                    break;
                case 22:
                    if (quality < 7)
                        SwapLight(flat, 11);
                    else if (quality < 12)
                        SwapLight(flat, 26);
                    else if (quality > 15)
                        SwapLight(flat, 13);
                    break;
                case 24:
                case 25:
                case 26:
                    if (quality > 10 && buildingType == DFLocation.BuildingTypes.Tavern && TemperedInteriorsMod.Instance.IsVisibleToProprietor(flat))
                    {
                        Vector3 groundPos = Utility.FindGround(flat.transform.position);
                        Vector3 lightBottomPos = FindBottom(flat);
                        if ((lightBottomPos - groundPos).magnitude > 3f)
                            SwapLight(flat, quality < 18 ? 23 : 9, Alignment.Bottom); //chandelier
                    }
                    else if (quality > 14)
                        SwapLight(flat, 13); //globe light
                    else if (quality < 6)
                        SwapLight(flat, 11, Alignment.Bottom); //simple hanging candle
                    break;
                default:
                    break;

            }
        }


        /// <summary>
        /// Examines the billboard size and location to find the bottom of the billboard.
        /// </summary>
        static Vector3 FindBottom(DaggerfallBillboard flat)
        {
            float yExtent = flat.GetComponent<MeshRenderer>().bounds.extents.y;
            Vector3 pos = flat.transform.position;
            return new Vector3(pos.x, pos.y - yExtent, pos.z);
        }


        /// <summary>
        /// Create new light billboard object given texture record from archive 210
        /// </summary>
        static void SwapLight(DaggerfallBillboard flat, int record, Alignment alignment = Alignment.Top)
        {
            GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(210, record, flat.transform.parent);

            if (record == 22 && Utility.GetHeight(flat.gameObject) < 2.8f)
            {
                go.transform.localScale *= 0.75f;
            }

            AddLight(record, go);

            Utility.SwapFlat(flat, go, alignment);

            //attach additional cord/chain if necessary
            AddSupport(go, record);
        }


        /// <summary>
        /// Adds enough cord/chain to connect light to ceiling.
        /// </summary>
        static void AddSupport(GameObject go, int record)
        {
            if (record != 11 && record != 9 && record != 23)
                return;

            //Calculate length of support needed
            Vector3 ceiling = Utility.FindCeiling(go.transform.position);
            float distance = Vector3.Distance(go.transform.position, ceiling);
            Vector3 extents = go.GetComponent<MeshRenderer>().bounds.extents;
            float length = distance - extents.y;
            if (length < 0.1f)
                return;

            //create length of cord/chain to attach top of light to ceiling
            GameObject support = new GameObject("Tempered Interiors Light Support");
            support.transform.parent = go.transform;
            support.transform.localPosition = Vector3.up * (length/2 + extents.y);

            Texture2D supportTexture = record == 11 ? Textures.Cord.Get() : Textures.Chain.Get();

            DaggerfallBillboard dfBillboard = support.AddComponent<DaggerfallBillboard>();
            float width = record == 11 ? 0.5f : 0.1f;
            Vector2 supportSize = new Vector2(width, length);
            dfBillboard.SetMaterial(supportTexture, supportSize, false);

            MeshRenderer meshRenderer = support.GetComponent<MeshRenderer>();

            //tiling texture
            meshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, length * 10);

        }


        /// <summary>
        /// Adds interior point light. (Mostly copied from DaggerfallInterior)
        /// </summary>
        static void AddLight(int record, GameObject flat)
        {
            Transform parent = flat.transform;

            if (DaggerfallUnity.Instance.Option_InteriorLightPrefab == null)
                return;

            // Create gameobject
            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_InteriorLightPrefab.gameObject, string.Empty, parent, Vector3.zero);

            // Set local position to billboard origin, otherwise light transform is at base of billboard
            go.transform.localPosition = Vector3.zero;

            // Adjust position of light for standing lights as their source comes more from top than middle
            Vector2 size = DaggerfallUnity.Instance.MeshReader.GetScaledBillboardSize(210, record) * MeshReader.GlobalScale;
            switch (record)
            {
                case 0:         // Bowl with fire
                    go.transform.localPosition += new Vector3(0, -0.1f, 0);
                    break;
                case 1:         // Campfire
                    // todo
                    break;
                case 2:         // Skull candle
                    go.transform.localPosition += new Vector3(0, 0.1f, 0);
                    break;
                case 3:         // Candle
                    go.transform.localPosition += new Vector3(0, 0.1f, 0);
                    break;
                case 4:         // Candle in bowl
                    // todo
                    break;
                case 5:         // Candleholder with 3 candles
                    go.transform.localPosition += new Vector3(0, 0.15f, 0);
                    break;
                case 6:         // Skull torch
                    go.transform.localPosition += new Vector3(0, 0.6f, 0);
                    break;
                case 7:         // Wooden chandelier with extinguished candles
                    // todo
                    break;
                case 8:         // Turkis lamp
                    // do nothing
                    break;
                case 9:        // Metallic chandelier with burning candles
                    go.transform.localPosition += new Vector3(0, 0.4f, 0);
                    break;
                case 10:         // Metallic chandelier with extinguished candles
                    // todo
                    break;
                case 11:        // Candle in lamp
                    go.transform.localPosition += new Vector3(0, -0.4f, 0);
                    break;
                case 12:         // Extinguished lamp
                    // todo
                    break;
                case 13:        // Round lamp (e.g. main lamp in mages guild)
                    go.transform.localPosition += new Vector3(0, -0.35f, 0);
                    break;
                case 14:        // Standing lantern
                    go.transform.localPosition += new Vector3(0, size.y / 2, 0);
                    break;
                case 15:        // Standing lantern round
                    go.transform.localPosition += new Vector3(0, size.y / 2, 0);
                    break;
                case 16:         // Mounted Torch with thin holder
                    // todo
                    break;
                case 17:        // Mounted torch 1
                    go.transform.localPosition += new Vector3(0, 0.2f, 0);
                    break;
                case 18:         // Mounted Torch 2
                    // todo
                    break;
                case 19:         // Pillar with firebowl
                    // todo
                    break;
                case 20:        // Brazier torch
                    go.transform.localPosition += new Vector3(0, 0.6f, 0);
                    break;
                case 21:        // Standing candle
                    go.transform.localPosition += new Vector3(0, size.y / 2.4f, 0);
                    break;
                case 22:         // Round lantern with medium chain
                    go.transform.localPosition += new Vector3(0, -0.5f, 0);
                    break;
                case 23:         // Wooden chandelier with burning candles
                    // todo
                    break;
                case 24:        // Lantern with long chain
                    go.transform.localPosition += new Vector3(0, -1.85f, 0);
                    break;
                case 25:        // Lantern with medium chain
                    go.transform.localPosition += new Vector3(0, -1.0f, 0);
                    break;
                case 26:        // Lantern with short chain
                    // todo
                    break;
                case 27:        // Lantern with no chain
                    go.transform.localPosition += new Vector3(0, -0.02f, 0);
                    break;
                case 28:        // Street Lantern 1
                    // todo
                    break;
                case 29:        // Street Lantern 2
                    // todo
                    break;
            }

            // adjust properties of light sources (e.g. Shrink light radius of candles)
            Light light = go.GetComponent<Light>();
            switch (record)
            {
                case 0:         // Bowl with fire
                    light.range = 20.0f;
                    light.intensity = 1.1f;
                    light.color = new Color(0.95f, 0.91f, 0.63f);
                    break;
                case 1:         // Campfire
                    light.range = 20.0f;
                    light.intensity = 1.0f;
                    light.color = new Color(1.0f, 0.91f, 0.63f);
                    break;
                case 2:         // Skull candle
                    light.range /= 3f;
                    //light.intensity = 0.6f;
                    light.color = new Color(1.0f, 0.99f, 0.82f);
                    break;
                case 3:         // Candle
                    light.range /= 3f;
                    break;
                case 4:         // Candle with base
                    light.range /= 3f;
                    break;
                case 5:         // Candleholder with 3 candles
                    light.range = 7.5f;
                    //light.intensity = 0.33f;
                    light.color = new Color(1.0f, 0.89f, 0.61f);
                    break;
                case 6:         // Skull torch
                    light.range = 15.0f;
                    light.intensity = 0.75f;
                    light.color = new Color(1.0f, 0.93f, 0.62f);
                    break;
                case 7:         // Wooden chandelier with extinguished candles
                    light.range = 0f;
                    break;
                case 8:         // Turkis lamp
                    light.color = new Color(0.68f, 1.0f, 0.94f);
                    break;
                case 9:        // metallic chandelier with burning candles
                    light.range = 15.0f;
                    //light.intensity = 0.65f;
                    light.color = new Color(1.0f, 0.92f, 0.6f);
                    break;
                case 10:         // Metallic chandelier with extinguished candles
                    light.range = 0f;
                    break;
                case 11:        // Candle in lamp
                    light.range = 5.0f;
                    light.intensity = 0.8f;
                    break;
                case 12:         // Extinguished lamp
                    light.range = 0f;
                    break;
                case 13:        // Round lamp (e.g. main lamp in mages guild)
                    light.range *= 1.2f;
                    light.intensity = 1.1f;
                    light.color = new Color(0.93f, 0.84f, 0.49f);
                    break;
                case 14:        // Standing lantern
                    // todo
                    break;
                case 15:        // Standing lantern round
                    // todo
                    break;
                case 16:         // Mounted Torch with thin holder
                    // todo
                    break;
                case 17:        // Mounted torch 1
                    light.intensity = 0.8f;
                    light.color = new Color(1.0f, 0.97f, 0.87f);
                    break;
                case 18:         // Mounted Torch 2
                    // todo
                    break;
                case 19:         // Pillar with firebowl
                    // todo
                    break;
                case 20:        // Brazier torch
                    light.range = 12.0f;
                    light.intensity = 0.75f;
                    light.color = new Color(1.0f, 0.92f, 0.72f);
                    break;
                case 21:        // Standing candle
                    light.range /= 3f;
                    light.intensity = 0.5f;
                    light.color = new Color(1.0f, 0.95f, 0.67f);
                    break;
                case 22:         // Round lantern with medium chain
                    light.intensity = 1.4f;
                    light.color = new Color(1.0f, 0.95f, 0.78f);
                    break;
                case 23:         // Wooden chandelier with burning candles
                    light.range = 15.0f;
                    //light.intensity = 0.65f;
                    light.color = new Color(1.0f, 0.92f, 0.6f);
                    break;
                case 24:        // Lantern with long chain
                    light.range = 10.0f;
                    light.intensity = 1.2f;
                    light.color = new Color(1.0f, 0.98f, 0.64f);
                    break;
                case 25:        // Lantern with medium chain
                    light.range = 10.0f;
                    light.intensity = 1.2f;
                    light.color = new Color(1.0f, 0.98f, 0.64f);
                    break;
                case 26:        // Lantern with short chain
                    light.range = 10.0f;
                    light.intensity = 1.2f;
                    light.color = new Color(1.0f, 0.98f, 0.64f);
                    break;
                case 27:        // Lantern with no chain
                    light.range = 10.0f;
                    light.intensity = 1.2f;
                    light.color = new Color(1.0f, 0.98f, 0.64f);
                    break;
                case 28:        // Street Lantern 1
                    // todo
                    break;
                case 29:        // Street Lantern 2
                    // todo
                    break;
            }

        }

    } //class Lighting


} //namespace
