// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;


namespace TemperedInteriors
{
    public enum Textures
    {
        BedEndHi,
        BedEndLow,
        BedSideHi,
        BedSideLow,
        BedTopHi,
        BedTopLow,
        WardrobeFrontHi,
        WardrobeFrontEdgeHi,
        WardrobeSideEdgeHi,
        WardrobeSideHi,
        DoorHi,
        CarpetLow,
        CarpetEdgeLow1,
        CarpetEdgeLow2,
        TapestryLow,
        Cord,
        Chain,
        Stain,
        FoodBit

    } //enum Textures


    public static class TexturesExtension
    {
        static readonly Dictionary<Textures, Texture2D> dict = new Dictionary<Textures, Texture2D>();

        /// <summary>
        /// </summary>
        public static Texture2D Get(this Textures key)
        {
            return dict[key];
        }


        /// <summary>
        /// Attempts to load all textures specified in the Textures enum.
        /// </summary>
        public static void Load()
        {
            bool allTexturesLoaded = true;
            
            ModManager modManager = ModManager.Instance;

            foreach (Textures key in Enum.GetValues(typeof(Textures)))
            {
                if (modManager.TryGetAsset(key.ToString(), false, out Texture2D texture))
                {
                    texture.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;
                    dict.Add(key, texture);
                }
                else
                {
                    Debug.LogWarningFormat("Unable to load texture for key '{0}'", key);
                    allTexturesLoaded = false;
                }
            }

            if (allTexturesLoaded)
                Debug.Log("All textures successfully loaded");
        }


    } //class TextExtension



} //namespace
