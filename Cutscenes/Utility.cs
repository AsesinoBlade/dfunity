// Project:         Cutscene animation for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace Cutscene
{


    public static class Utility
    {

        /// <summary>
        /// Gets texture for actor paper doll.
        /// </summary>
        public static Texture2D GetPaperDoll(ActorSpecifier actor)
        {
            //just rendering basic player paper doll for now
            PaperDollRenderer paperDoll = new PaperDollRenderer();
            paperDoll.Refresh();
            return paperDoll.PaperDollTexture;
        }


        /// <summary>
        /// Creates a tiled texture composed of one or more other textures.
        /// A tile unit is created by combining the provided textures.
        /// This tile unit is then repeated by the width and height.
        /// </summary>
        public static Texture2D CreateTiledTexture(TiledTexture tiled)
        {
            int sumWidth = 0;
            int maxHeight = 0;

            Texture2D fillTexture = GetTexture(tiled.FillTexture);

            List<Texture2D> textures = new List<Texture2D>();
            foreach (TextureSpecifier spec in tiled.Tiles)
                textures.Add(GetTexture(spec));

            foreach (Texture2D tex in textures)
            {
                sumWidth += tex.width;
                if (tex.height > maxHeight)
                    maxHeight = tex.height;
            }

            if (sumWidth == 0)
            {
                //no texture records, size will be entirely determined by the fillTexture

                if (fillTexture == null || fillTexture.width == 0)
                    throw new CutsceneException("Must supply a valid texture");

                maxHeight = fillTexture.height;
                sumWidth = fillTexture.width;
            }

            Texture2D textureBlock = new Texture2D(sumWidth, maxHeight);
            if (fillTexture != null)
                FillWithTexture(fillTexture, textureBlock);

            int xOffset = 0;
            foreach (Texture2D tex in textures)
            {
                //HideInInspector will be set for archive 0 record 0, this is a 'no effect' texture
                if (tex.hideFlags != HideFlags.HideInInspector)
                    Graphics.CopyTexture(tex, 0, 0, 0, 0, tex.width, tex.height, textureBlock, 0, 0, xOffset, 0);

                xOffset += tex.width;
            }


            int totalHeight = maxHeight * tiled.Height;
            int totalWidth = sumWidth * tiled.Width;

            Texture2D combinedTexture = new Texture2D(totalWidth, totalHeight);
            FillWithTexture(textureBlock, combinedTexture);

            return combinedTexture;
        }


        /// <summary>
        /// Retrieves the texture corresponding to the provided texture specifier.
        /// </summary>
        public static Texture2D GetTexture(TextureSpecifier specifier)
        {
            if (specifier.IsCustom)
            {
                return GetTexture(specifier.Custom);
            }
            else
            {
                (int archive, int record, int frame) = specifier.Archived;
                return GetTexture(archive, record, frame);
            }
        }


        /// <summary>
        /// Retrieves the texture corresponding to the provided archive, record, and frame.
        /// The frame value is only relevant for animated textures, when a single frame is desired.
        /// </summary>
        public static Texture2D GetTexture(int archive, int record, int frame)
        {
            TextureReader reader = DaggerfallUnity.Instance.MaterialReader.TextureReader;

            Texture2D texture = reader.GetTexture2D(archive, record, frame >= 0 ? frame : 0);

            if (texture == null || texture.height == 0)
                throw new CutsceneException(string.Format("Failed TextureReader.GetTexture2D(), texture (archive:{0} record:{1}) not found.", archive, record));

            //for texture 0:0, set a flag to prevent drawing when tiling
            if (archive == 0 && record == 0)
                texture.hideFlags = HideFlags.HideInInspector;

            return texture;
        }


        /// <summary>
        /// Retrieves the custom texture for the specified filename.
        /// </summary>
        public static Texture2D GetTexture(string textureName)
        {
            if (!TextureReplacement.TryImportTexture(textureName, true, out Texture2D texture))
                texture = Resources.Load<Texture2D>(textureName);

            if (texture == null)
                throw new CutsceneException(string.Format("Failed TextureReplacement Import and Resources.Load for texture '{0}'.", textureName));

            return texture;
        }


        /// <summary>
        /// Fills the dst texture by copying the src texture one or more times into it.
        /// </summary>
        public static void FillWithTexture(Texture2D src, Texture2D dst)
        {
            int yOffset = 0;

            while (yOffset < dst.height)
            {
                int xOffset = 0;

                while (xOffset < dst.width)
                {
                    int srcWidth = src.width;
                    int srcHeight = src.height;

                    //prevent exceeding the boundaries of the destination texture
                    if (xOffset + srcWidth > dst.width)
                        srcWidth = dst.width - xOffset;

                    if (yOffset + srcHeight > dst.height)
                        srcHeight = dst.height - yOffset;

                    Graphics.CopyTexture(src, 0, 0, 0, 0, srcWidth, srcHeight, dst, 0, 0, xOffset, yOffset);

                    xOffset += src.width;
                }

                yOffset += src.height;
            }
        }


    } //class Utility



    public class ActorSpecifier
    {
        readonly int pcNum;
        readonly int race;
        readonly int sex;
        readonly int face;
        readonly List<(int, int, int)> equipment;

        public ActorSpecifier(int pcNum, List<(int, int, int)> equipment)
        {
            this.pcNum = pcNum;
            this.equipment = equipment;
        }
    }


    public class TiledTexture
    {
        readonly int width;
        readonly int height;
        readonly TextureSpecifier fillTexture;
        readonly List<TextureSpecifier> tileTextures;

        public TiledTexture(int width, int height, TextureSpecifier fillTexture, List<TextureSpecifier> tileTextures)
        {
            this.width = width;
            this.height = height;
            this.fillTexture = fillTexture;
            this.tileTextures = tileTextures;
        }

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public TextureSpecifier FillTexture { get { return fillTexture; } }
        public List<TextureSpecifier> Tiles { get { return tileTextures; } }
    }


    public class TextureSpecifier
    {
        (int, int, int) archived;
        readonly string custom;

        public TextureSpecifier((int, int, int) archived)
        {
            this.archived = archived;
        }

        public TextureSpecifier(string custom)
        {
            this.custom = custom;
        }

        public bool IsCustom { get { return custom != null; } }
        public (int, int, int) Archived { get { return archived; } }
        public string Custom {  get { return custom; } }
    }


    public enum CutscenePropertyType
    {
        Time,
        Tint,
        X,
        Y,
        Z,
        XRot,
        YRot,
        ZRot,
        Scale,
        Volume,
        Pitch,
        Balance
    } //enum



    public class CutsceneProperties : List<CutsceneProperty>
    {
        public new void Add(CutsceneProperty property)
        {
            if (Exists(p => p.Type == property.Type))
                throw new CutsceneException(string.Format("Duplicate property '{0}'", property.Type.ToString()));

            base.Add(property);
        }

        public CutsceneProperty Get(CutscenePropertyType type, bool returnNullIfMissing = false)
        {
            if (Exists(p => p.Type == type))
                return Find(p => p.Type == type);
            else if (returnNullIfMissing)
                return null;
            else
                return new CutsceneProperty(type);

        }
    }


    public class CutsceneProperty
    {
        public CutscenePropertyType Type;
        public float StartValue;
        public float EndValue;
        public float Cycles;
        public Color StartColor;
        public Color EndColor;

        public CutsceneProperty(CutscenePropertyType type)
        {
            Type = type;
            StartValue = EndValue = Cycles = 0;
            StartColor = EndColor = Color.white;
        }

        /// <summary>
        /// Returns true if the start value and end value are different
        /// </summary>
        public bool IsRange()
        {
            return StartValue != EndValue || StartColor != EndColor;
        }

    }//class CutsceneProperty




    /// <summary>
    /// General Cutscene Exception
    /// </summary>
    public class CutsceneException : Exception
    {
        public CutsceneException(string msg) : base(msg)
        {
        }
    } //class CutsceneException




} //namespace
