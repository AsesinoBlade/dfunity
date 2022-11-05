// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using UnityEngine;
using DaggerfallWorkshop;


namespace TemperedInteriors
{

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class StaticBillboard : MonoBehaviour
    {
        /// <summary>
        /// Sets the material texture used by the billboard object.
        /// </summary>
        public Material SetMaterial(Texture2D texture, Vector2 size)
        {
            // Get DaggerfallUnity
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Get references
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            
            // Create material
            Material material = MaterialReader.CreateStandardMaterial(MaterialReader.CustomBlendMode.Fade);
            material.mainTexture = texture;

            // Create mesh
            Mesh mesh = dfUnity.MeshReader.GetSimpleBillboardMesh(size);

            // Assign mesh and material
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Mesh oldMesh = meshFilter.sharedMesh;
            if (mesh)
            {
                meshFilter.sharedMesh = mesh;
                meshRenderer.sharedMaterial = material;
            }
            if (oldMesh)
            {
                // The old mesh is no longer required
#if UNITY_EDITOR
                DestroyImmediate(oldMesh);
#else
                Destroy(oldMesh);
#endif
            }

            // General billboard shadows if enabled
            //meshRenderer.shadowCastingMode = (DaggerfallUnity.Settings.GeneralBillboardShadows && !isLightArchive) ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;

            return material;
        }
    }


}
