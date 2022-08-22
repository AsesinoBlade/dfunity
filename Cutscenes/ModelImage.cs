// Project:         Cutscene animation for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using UnityEngine;
using UnityEngine.UI;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace Cutscene
{
    public class ModelImage : Image
    {
        class Details
        {
            public BillboardSummary summary;
            public int frames;
            public int archive;
            public int record;
            public bool hasMaterial;
            public float scale = 1;
        }

        const float basePixelsPerUnit = 100.0f;
        const int normalFps = 5;
        const int lightFps = 12;
        const int spellImpactFps = 15;

        static readonly Vector2 middleOfSprite = new Vector2(0.5f, 0.5f);

        static GameObject staticGameObject;

        Details details;
        int fps = normalFps;
        float lastUpdateTime;



        /// <summary>
        /// Checks if specified texture exists, throws CutsceneException if not.
        /// </summary>
        public static void CheckTextureExists(TextureSpecifier spec)
        {
            if (spec.IsCustom)
                CheckTextureExists(spec.Custom);
            else
                CheckTextureExists(spec.Archived);
        }


        /// <summary>
        /// Checks if specified texture exists, throws CutsceneException if not.
        /// </summary>
        public static void CheckTextureExists((int archive, int record, int frame) archived)
        {
            CheckTextureExists(archived.archive, archived.record, archived.frame);
        }


        /// <summary>
        /// Checks if specified texture exists, throws CutsceneException if not.
        /// </summary>
        public static void CheckTextureExists(int archive, int record, int frame = -1)
        {
            Details details = GetTextureDetails(archive, record, frame >= 0 ? frame : 0);

            if (details.hasMaterial == false || frame > details.frames)
            {
                string msg = string.Format("Failed to find texture archive:{0} record:{1}", archive, record);
                if (frame >= 0)
                    msg = string.Format("Failed to find texture archive:{0} record:{1} frame:{2}", archive, record, frame);

                throw new CutsceneException(msg);
            }
        }

        /// <summary>
        /// Checks if specified custom texture file exists, throws CutsceneException if not.
        /// </summary>
        public static void CheckTextureExists(string textureName)
        {
            Utility.GetTexture(textureName);
        }


        /// <summary>
        /// Sets the texture for this model.
        /// If frame is not specified, animated textures will be animated.
        /// </summary>
        public void SetTexture(int archive, int record, int frame = -1)
        {
            details = GetTextureDetails(archive, record);

            if (frame >= 0)
                details.summary.AnimatedMaterial = false;

            details.summary.CurrentFrame = frame >= 0 ? frame : 0;

            CreateSprite();
            AdjustSizeAndScale(false);

            fps = normalFps;
            if (archive == TextureReader.LightsTextureArchive)
                fps = lightFps;
            else if (archive >= 375 && archive <= 379)
                fps = spellImpactFps;

            lastUpdateTime = Time.realtimeSinceStartup;
        }


        /// <summary>
        /// Sets the texture for this model.
        /// </summary>
        public void SetTexture(Texture2D texture, bool isPaperDoll)
        {
            texture.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;
            texture.wrapMode = TextureWrapMode.Clamp;

            details = new Details
            {
                summary = new BillboardSummary
                {
                    AnimatedMaterial = false,
                    AtlasedMaterial = false
                }
            };

            Rect rect = new Rect(0, 0, texture.width, texture.height);
            
            sprite = Sprite.Create(texture, rect, middleOfSprite, basePixelsPerUnit);

            AdjustSizeAndScale(isPaperDoll);
        }


        void AdjustSizeAndScale(bool isPaperDoll)
        {
            float nativeAspectRatio = 320f / 200f;
            float currentAspectRatio = (float)Screen.width / (float)Screen.height;

            float newHeight = sprite.textureRect.height;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

            float aspectAdjustment = isPaperDoll ? currentAspectRatio / nativeAspectRatio : nativeAspectRatio / currentAspectRatio;
            float newWidth = sprite.textureRect.width * aspectAdjustment;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

            //seems like a confortable default scale
            details.scale = (float)Screen.width / 320f / 1.2f;

            transform.localScale = Vector3.one * GetScale();
        }


        /// <summary>
        /// Gets the default draw scale, used in Clip.Change() scaling.
        /// </summary>
        public float GetScale()
        {
            return details.scale;
        }


        /// <summary>
        /// For animated models, sets the current frame
        /// </summary>
        public void UpdateFrame()
        {
            if (details.summary.AnimatedMaterial == false)
                return;


            float updateTime = 1f / fps;
            if (Time.realtimeSinceStartup >= lastUpdateTime + updateTime)
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                ++details.summary.CurrentFrame;
                CreateSprite();
            }

        }

        /// <summary>
        /// Creates a sprite for the model.
        /// If it is an animated model, this should be called whenever a frame update is needed.
        /// </summary>
        void CreateSprite()
        {
            Texture2D texture;

            if (details.summary.ImportedTextures.HasImportedTextures)
            {
                if (details.summary.CurrentFrame >= details.summary.ImportedTextures.FrameCount)
                    details.summary.CurrentFrame = 0;

                // Set imported textures for current frame
                texture = details.summary.ImportedTextures.Albedo[details.summary.CurrentFrame];
                if (details.summary.ImportedTextures.IsEmissive)
                    texture = details.summary.ImportedTextures.Emission[details.summary.CurrentFrame];
            }
            else
            {
                if (details.summary.CurrentFrame >= details.summary.AtlasIndices[details.record].frameCount)
                    details.summary.CurrentFrame = 0;

                TextureReader textureReader = DaggerfallUnity.Instance.MaterialReader.TextureReader;
                texture = textureReader.GetTexture2D(details.archive, details.record, details.summary.CurrentFrame);
            }


            texture.filterMode = texture.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;
            texture.wrapMode = TextureWrapMode.Clamp;

            Rect rect = new Rect(0, 0, texture.width, texture.height);

            sprite = Sprite.Create(texture, rect, middleOfSprite, basePixelsPerUnit);
        }


        /// <summary>
        /// Gets texture info used by ModelImage from custom file or atlas.
        /// </summary>
        static Details GetTextureDetails(int archive, int record, int frame = 0)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

            if (!staticGameObject)
            {
                staticGameObject = new GameObject("static ModelImage");
                staticGameObject.SetActive(false);
            }

            staticGameObject.transform.localScale = Vector3.one;

            Details details = new Details();

            BillboardSummary summary = new BillboardSummary();

            Material material;

            material = TextureReplacement.GetStaticBillboardMaterial(staticGameObject, archive, record, ref summary, out _);
            if (material)
            {
                dfUnity.MeshReader.GetBillboardMesh(summary.Rect, archive, record, out _);
                details.frames = summary.ImportedTextures.FrameCount;

                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = details.frames > 1;
            }
            else if (dfUnity.MaterialReader.AtlasTextures)
            {
                material = dfUnity.MaterialReader.GetMaterialAtlas(
                    archive,
                    0,
                    4,
                    2048,
                    out summary.AtlasRects,
                    out summary.AtlasIndices,
                    4,
                    true,
                    0,
                    false,
                    true);


                if (material != null)
                {
                    if (record < summary.AtlasIndices.Length)
                    {
                        details.frames = summary.AtlasIndices[record].frameCount;

                        summary.AtlasedMaterial = true;
                        summary.AnimatedMaterial = details.frames > 1;
                    }
                    else
                    {
                        material = null;
                    }
                }

            }
            else
            {
                material = dfUnity.MaterialReader.GetMaterial(
                    archive,
                    record,
                    frame,
                    0,
                    out summary.Rect,
                    4,
                    true,
                    true);

                if (material != null)
                {
                    details.frames = frame + 1;

                    summary.AtlasedMaterial = false;
                    summary.AnimatedMaterial = false;
                }
            }

            details.archive = archive;
            details.record = record;

            details.hasMaterial = material != null;
            details.summary = summary;

            return details;
        }


    } //class ModelImage


    class ModelBehaviour : MonoBehaviour
    {
        void Update()
        {
            //give ModelImage an opportunity to update animated textures
            GetComponent<ModelImage>().UpdateFrame();
        }
    }


} //namespace


