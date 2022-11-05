// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;


namespace TemperedInteriors
{
    public enum Alignment
    {
        Top,
        Bottom,
        Middle,
        Ground
    }

    public static class Utility
    {
        static readonly Regex rgxTextureName = new Regex(@"(\d+)\D+(\d+)");
        static readonly DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
        static readonly Dictionary<System.Object, Texture2D> textureDict = new Dictionary<object, Texture2D>();
        static System.Random random;


        /// <summary>
        /// Initializes a random number generator with a seed value, to get consistently reproducible results.
        /// </summary>
        public static void Init(int seed)
        {
            random = new System.Random(seed);
        }


        /// <summary>
        /// Generates a floating point value in the specified range.
        /// </summary>
        public static float Random(float min, float max)
        {
            float range = max - min;
            if (range <= 0)
                return min;

            float scale = (float)random.NextDouble();
            float offset = range * scale;

            float value = min + offset;
            return value;
        }


        /// <summary>
        /// Generates a int value in the specified range, inclusive of min, exclusive of max.
        /// </summary>
        public static int Random(int min, int max)
        {
            int value = random.Next(min, max);
            return value;
        }


        /// <summary>
        /// Generates a hash value for a specific location in a building interior.
        /// </summary>
        public static uint GenerateHashValue(DaggerfallInterior interior, Vector3 location)
        {
            uint hash = (uint)interior.name.GetHashCode();
            hash += (uint)(location.x * 10);
            hash += (uint)(location.y * 10);
            hash += (uint)(location.z * 10);

            return hash;
        }


        /// <summary>
        /// Extracts the texture archive/record numbers from the name of the main material.
        /// </summary>
        public static (int,int) ExtractMainTextureValue(MeshRenderer renderer)
        {
            Match match = rgxTextureName.Match(renderer.material.ToString());
            if (match.Groups.Count != 3)
                return (0, 0);

            (int, int) textureValue = (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));

            return textureValue;
        }


        /// <summary>
        /// Gets height of object above the floor.
        /// </summary>
        public static float GetHeight(GameObject go)
        {
            return GetHeight(go.transform.position);
        }


        /// <summary>
        /// Gets height of location above the floor.
        /// </summary>
        public static float GetHeight(Vector3 location)
        {
            if (Physics.Raycast(location, Vector3.down, out RaycastHit hitInfo))
                return Vector3.Distance(location, hitInfo.point);
            else
                return 999;
        }


        /// <summary>
        /// Finds the location of the lower surface beneath a location.
        /// </summary>
        public static Vector3 FindGround(Vector3 location)
        {
            if (Physics.Raycast(location, Vector3.down, out RaycastHit hitInfo))
                return hitInfo.point;
            else
                return location;
        }


        /// <summary>
        /// Finds the location of the upper surface above a location.
        /// </summary>
        public static Vector3 FindCeiling(Vector3 location)
        {
            if (Physics.Raycast(location, Vector3.up, out RaycastHit hitInfo))
                return hitInfo.point;
            else
                return location;
        }


        /// <summary>
        /// Replaces a billboard with a new billboard using the supplied texture archive/record.
        /// </summary>
        public static void SwapFlat(DaggerfallBillboard flat, (int, int) replacement, Alignment alignment = Alignment.Bottom)
        {
            (int archive, int record) = replacement;

            GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(archive, record, flat.transform.parent);
            go.name = flat.name + " (Tempered Interiors Replacement)";

            SwapFlat(flat, go, alignment);
        }


        /// <summary>
        /// Swap billboard objects, making height adjustment as necessary, and destroys the old billboard.
        /// </summary>
        public static void SwapFlat(DaggerfallBillboard flat, GameObject newFlatObject, Alignment alignment = Alignment.Bottom)
        {
            DaggerfallBillboard newFlat = newFlatObject.GetComponent<DaggerfallBillboard>();

            Vector3 pos = flat.transform.position;

            float height1 = flat.Summary.Size.y;
            float height2 = newFlat.Summary.Size.y * newFlat.transform.localScale.y;
            float yAdjustment = (height1 - height2) / 2;

            if (alignment == Alignment.Bottom)
                newFlat.transform.position = new Vector3(pos.x, pos.y - yAdjustment, pos.z);
            else if (alignment == Alignment.Top)
                newFlat.transform.position = new Vector3(pos.x, pos.y + yAdjustment, pos.z);
            else if (alignment == Alignment.Ground)
                newFlat.transform.position = FindGround(flat.transform.position) + Vector3.up * height2 / 2;
            else
                newFlat.transform.position = pos;


            GameObject.Destroy(flat.gameObject); //destroy original object that was replaced
        }


        /// <summary>
        /// Potentially scales the texture and stores it, or returns existing texture.
        /// </summary>
        public static Texture2D GetStashedTexture(Textures desired, bool scaleDown)
        {
            if (textureDict.ContainsKey(desired))
            {
                return textureDict[desired];
            }
            else
            {
                Texture2D tex = desired.Get();
                tex.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;

                if (scaleDown)
                    tex = ScaleTextureDown(tex);

                textureDict.Add(desired, tex);

                return tex;
            }
        }


        /// <summary>
        /// Potentially scales the texture (from given archive/record) and stores it, or returns existing texture.
        /// </summary>
        public static Texture2D GetStashedTexture((int,int) desired, bool scaleDown)
        {
            if (textureDict.ContainsKey(desired))
            {
                return textureDict[desired];
            }
            else
            {
                (int archive, int record) = desired;
                Texture2D tex = dfUnity.MaterialReader.TextureReader.GetTexture2D(archive, record);
                tex.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;

                if (scaleDown)
                    tex = ScaleTextureDown(tex);

                textureDict.Add(desired, tex);

                return tex;
            }
        }


        public static void UseGhostShader(GameObject model)
        {
            MeshRenderer renderer = model.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find(MaterialReader._DaggerfallGhostShaderName);

            Material[] materials = new Material[renderer.materials.Length];

            for (int i = 0; i < materials.Length; ++i)
            {
                materials[i] = renderer.materials[i];
                materials[i].shader = shader;
                materials[i].SetFloat("_Cutoff", 0.1f);
            }

            renderer.materials = materials;
        }


        /// <summary>
        /// Replaces a specific model texture (specified by archive/record) with a new texture.
        /// </summary>
        public static void SwapModelTexture(GameObject model, (int, int) searchVal, Textures replacement, bool scaleDown = false)
        {
            Texture2D tex = GetStashedTexture(replacement, scaleDown);

            if (tex)
                SwapModelTexture(model, searchVal, tex);
        }


        /// <summary>
        /// Replaces a specific model texture (specified by archive/record) with a new texture (archive/record).
        /// </summary>
        public static void SwapModelTexture(GameObject model, (int, int) searchVal, (int,int) replacement, bool scaleDown = false)
        {
            Texture2D tex = GetStashedTexture(replacement, scaleDown);

            if (tex)
                SwapModelTexture(model, searchVal, tex);
        }


        /// <summary>
        /// Replaces a specific model texture (specified by archive/record) with a new texture.
        /// </summary>
        static void SwapModelTexture(GameObject model, (int,int) searchVal, Texture2D replacement)
        {
            MeshRenderer renderer = model.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Material[] materials = new Material[renderer.materials.Length];
            bool found = false;

            for (int i = 0; i < renderer.materials.Length; ++i)
            {
                Material mat = renderer.materials[i];
                materials[i] = mat;

                if (!found)
                {
                    Match match = rgxTextureName.Match(mat.ToString());
                    if (match.Groups.Count != 3)
                        continue;

                    (int, int) oldTexture = (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                    if (searchVal == oldTexture)
                    {
                        //disable other material properties another mod may have added
                        mat.DisableKeyword("_NORMALMAP");
                        mat.DisableKeyword("_PARALLAXMAP");
                        mat.SetTexture("_OcclusionMap", null);
                        mat.SetTexture("_ParallaxMap", null);
                        mat.SetTexture("_BumpMap", null);

                        mat.mainTexture = replacement;
                        materials[i] = mat;
                        found = true;
                    }
                }
            }

            if (found)
                renderer.materials = materials;
        }


        /// <summary>
        /// Reduces a texture to 1/4 size, used to create a more pixelated texture to match other game textures.
        /// </summary>
        static Texture2D ScaleTextureDown(Texture2D src)
        {
            int width = src.width / 2;
            int height = src.height / 2;

            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            Texture2D dst = new Texture2D(width, height);
            dst.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;

            try
            {
                RenderTexture.active = rt;
                Graphics.Blit(src, rt);

                dst.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                dst.Apply();
            }
            finally
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }

            return dst;
        }




    } //class Utility

} //namespace
