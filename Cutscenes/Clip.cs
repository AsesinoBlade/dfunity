// Project:         Cutscene animation for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;

namespace Cutscene
{

    /// <summary>
    /// Used to build and play cutscene clips
    /// </summary>
    public class Clip
    {
        static readonly string stageID = "stage";
        static readonly string stageLightID = "stagelight";

        readonly List<Model> models;
        readonly List<Caption> captions;
        readonly List<Sound> sounds;

        SongFiles song = SongFiles.song_none;
        bool hasMusic = false;

        int runningCoroutines;


        public SongFiles Song { get { return song; } }
        public bool HasMusic { get { return hasMusic; } }


        public Clip()
        {
            models = new List<Model>
            {
                new Model(stageLightID),
                new Model(stageID)
            };

            captions = new List<Caption>();
            sounds = new List<Sound>();
        }


        /// <summary>
        /// Creates a model for the specified id/name, using provided archived or custom texture
        /// </summary>
        public void CreateModel(string id, TextureSpecifier texture)
        {
            ModelImage.CheckTextureExists(texture);

            Model model = new Model(id, texture);

            models.Add(model);
        }


        /// <summary>
        /// Creates a model for the specified id/name, using tiled texture
        /// </summary>
        public void CreateModel(string id, TiledTexture tiled)
        {
            ModelImage.CheckTextureExists(tiled.FillTexture);

            foreach (TextureSpecifier spec in tiled.Tiles)
                ModelImage.CheckTextureExists(spec);

            Model model = new Model(id, tiled);

            models.Add(model);
        }


        /// <summary>
        /// Creates a model for the specified id/name, using provided actor(paperdoll) information
        /// </summary>
        public void CreateModel(string id, ActorSpecifier actor)
        {
            Model model = new Model(id, actor);

            models.Add(model);
        }


        /// <summary>
        /// Changes a property of the specified model.
        /// If Time and the property are a range, the change will be gradually changed (lerped) over time.
        /// </summary>
        public void Change(string modelID, CutsceneProperties properties)
        {
            Model model = GetModel(modelID);
            model.AddAnimation(properties);
        }


        /// <summary>
        /// Adds caption text to be displayed in the bottom bar of the view window over the specified time range
        /// </summary>
        public void AddCaption(TextFile.Token[] tokens, CutsceneProperties properties)
        {
            Caption caption = new Caption(tokens, properties);
            captions.Add(caption);
        }


        /// <summary>
        /// Sets the song to be played during the clip
        /// </summary>
        public void SetSong(SongFiles song)
        {
            this.song = song;
            hasMusic = true;
        }


        /// <summary>
        /// Plays the sound clip at the specified Time.
        /// Additional properties can be specified to manipulate volume, pitch, and balance.
        /// </summary>
        public void AddSound(SoundClips soundClip, CutsceneProperties properties)
        {
            Sound sound = new Sound(soundClip, properties);
            sounds.Add(sound);
        }


        /// <summary>
        /// Checks that a model with the provided id/name exists.
        /// </summary>
        public bool HasModel(string id)
        {
            return models.Exists(x => x.ID.Equals(id));
        }


        /// <summary>
        /// Calculates the duration of the clip.
        /// The calculation is performed by examining the Time property values used during animation.
        /// </summary>
        public float GetDuration()
        {
            float duration = 0;

            //find largest end-time value in the model animations
            foreach (Model model in models)
            {
                foreach (CutsceneProperties props in model.Animations)
                {
                    duration = Mathf.Max(duration, props.Get(CutscenePropertyType.Time).EndValue);
                }
            }

            //sound effect duration timing
            foreach (Sound sound in sounds)
            {
                duration = Mathf.Max(duration, sound.Properties.Get(CutscenePropertyType.Time).EndValue);
            }

            //caption display times
            foreach (Caption caption in captions)
            {
                duration = Mathf.Max(duration, caption.Properties.Get(CutscenePropertyType.Time).EndValue);
            }

            return duration;
        }


        /// <summary>
        /// Plays the clip.
        /// Instantiates the script defined models and animates them using coroutines.
        /// </summary>
        public void Play(ViewWindow window)
        {
            runningCoroutines = 0;

            try
            {
                Dictionary<Model, GameObject> modelObjectDict = InstantiateModels(window);

                ScheduleModelAnimations(window, modelObjectDict);

                ScheduleSounds(window);

                ScheduleCaptions(window);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                runningCoroutines = 0;
            }
        }


        /// <summary>
        /// Used to check if clip has finished playing.
        /// It does this by checking if any coroutines are running.
        /// </summary>
        public bool IsPlaying()
        {
            return runningCoroutines > 0;
        }


        /// <summary>
        /// Instantiates all the defined models and attaches them to the stage.
        /// </summary>
        Dictionary<Model, GameObject> InstantiateModels(ViewWindow window)
        {
            Dictionary<Model, GameObject> modelObjectDict = new Dictionary<Model, GameObject>();

            foreach (Model model in models)
            {
                GameObject modelObject = InstantiateModel(window, model);

                if (!model.ID.Equals(stageID) && !model.ID.Equals(stageLightID))
                {
                    window.AddModelObjectToStage(modelObject);

                    //initial position is middle of screen
                    RectTransform rtransform = modelObject.GetComponent<RectTransform>();
                    rtransform.localPosition = Vector3.zero;
                }

                modelObjectDict.Add(model, modelObject);
            }

            return modelObjectDict;
        }


        /// <summary>
        /// Constructs temporary game objects to use for the animation and adds them to the stage.
        /// Images/Sprites are added as components to the object for the model graphics.
        /// </summary>
        GameObject InstantiateModel(ViewWindow window, Model model)
        {
            if (model.ID.Equals(stageID))
                return window.Stage;
            else if (model.ID.Equals(stageLightID))
                return window.StageLight;

            GameObject modelObject = new GameObject(model.ID);

            ModelImage modelImage = modelObject.AddComponent<ModelImage>();
            modelObject.AddComponent<ModelBehaviour>();

            modelObject.SetActive(false); //inactive until changed

            model.SetTexture(modelImage);

            return modelObject;
        }



        /// <summary>
        /// Starts the coroutines that perform the animations for all the models.
        /// </summary>
        void ScheduleModelAnimations(ViewWindow window, Dictionary<Model, GameObject> modelObjects)
        {
            foreach (Model model in modelObjects.Keys)
            {
                foreach (CutsceneProperties properties in model.Animations)
                {
                    CutsceneProperty time = properties.Get(CutscenePropertyType.Time);

                    if (time.StartValue == 0 && time.EndValue == 0)
                    {
                        //change is performed at start
                        Change(window, modelObjects[model], properties, 0);
                    }
                    else
                    {
                        IEnumerator coroutine = Schedule(window, modelObjects[model], time, properties);
                        window.Stage.GetComponent<Image>().StartCoroutine(coroutine);
                    }
                }
            }
        }


        /// <summary>
        /// Starts a coroutine that performs a specific animation for a model.
        /// </summary>
        IEnumerator Schedule(ViewWindow window, GameObject modelObject, CutsceneProperty time, CutsceneProperties properties)
        {
            ++runningCoroutines;

            try
            {
                if (time.StartValue > 0)
                    yield return new WaitForSecondsRealtime(time.StartValue);

                float duration = time.EndValue - time.StartValue;
                float startTime = Time.unscaledTime;
                float endTime = Time.unscaledTime + duration;

                while (Time.unscaledTime <= endTime)
                {
                    float elapsedTime = Time.unscaledTime - startTime;

                    float lerpValue = 1;

                    if (duration > 0)
                    {
                        lerpValue = elapsedTime / duration;

                        if (time.Cycles > 0)
                        {
                            double radians = lerpValue * time.Cycles * Math.PI;
                            lerpValue = Mathf.Abs(Mathf.Sin((float)radians));
                        }
                    }

                    Change(window, modelObject, properties, lerpValue);
                    yield return null;
                }
            }
            finally
            {
                --runningCoroutines;
            }
        }


        /// <summary>
        /// Changes model properties, uses lerpValue if a value range is provided.
        /// </summary>
        void Change(ViewWindow window, GameObject modelObject, CutsceneProperties properties, float lerpValue)
        {
            modelObject.SetActive(true);

            RectTransform transform = modelObject.GetComponent<RectTransform>();

            foreach (CutsceneProperty property in properties)
            {
                Vector3 currentPosition = transform.localPosition;
                Vector3 currentRotation = transform.localEulerAngles;

                float newValue = Mathf.Lerp(property.StartValue, property.EndValue, lerpValue);

                switch (property.Type)
                {
                    case CutscenePropertyType.X:
                        //supplied value is using 100x100 coordinate system, translate to normal screen space
                        newValue = window.XOffset + newValue * window.XScaler;
                        transform.localPosition = new Vector3(newValue, currentPosition.y, currentPosition.z);
                        break;
                    case CutscenePropertyType.Y:
                        //supplied value is using 100x100 coordinate system, translate to normal screen space
                        newValue = window.YOffset + newValue * window.YScaler;
                        transform.localPosition = new Vector3(currentPosition.x, newValue, currentPosition.z);
                        break;
                    case CutscenePropertyType.Z:
                        //can't re-order the stage or stagelight
                        if (modelObject.transform.parent == window.Stage.transform)
                        {
                            int index = Mathf.Clamp(Mathf.RoundToInt(newValue), 0, window.Stage.transform.childCount - 1);
                            modelObject.transform.SetSiblingIndex(index);
                        }
                        break;
                    case CutscenePropertyType.XRot:
                        transform.localEulerAngles = new Vector3(newValue, currentRotation.y, currentRotation.z);
                        break;
                    case CutscenePropertyType.YRot:
                        transform.localEulerAngles = new Vector3(currentRotation.x, newValue, currentRotation.z);
                        break;
                    case CutscenePropertyType.ZRot:
                        transform.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, newValue);
                        break;
                    case CutscenePropertyType.Scale:
                        float imageScale = modelObject.GetComponent<ModelImage>().GetScale();
                        float scale = newValue * imageScale / 100f;
                        transform.localScale = new Vector3(scale, scale, 0);
                        break;
                    case CutscenePropertyType.Tint:
                        Color current = modelObject.GetComponent<Image>().color;
                        Color startColor = CombineColors(current, property.StartColor);
                        Color endColor = CombineColors(current, property.EndColor);
                        Color newColor = Color.Lerp(startColor, endColor, lerpValue);
                        modelObject.GetComponent<Image>().color = newColor;
                        break;
                    default:
                        break;
                }
            }
        }


        /// <summary>
        /// Combining colors.
        /// If a modifier's rgba value is -1, it is ignored; otherwise it supplants the original color.
        /// </summary>
        Color CombineColors(Color original, Color modifier)
        {
            float r = modifier.r == -1 ? original.r : modifier.r;
            float g = modifier.g == -1 ? original.g : modifier.g;
            float b = modifier.b == -1 ? original.b : modifier.b;
            float a = modifier.a == -1 ? original.a : modifier.a;

            return new Color(r, g, b, a);
        }


        /// <summary>
        /// Starts the coroutines that play the sounds.
        /// </summary>
        void ScheduleSounds(ViewWindow window)
        {
            foreach (Sound sound in sounds)
            {
                CutsceneProperty time = sound.Properties.Get(CutscenePropertyType.Time);
                IEnumerator coroutine = Schedule(window, time, sound);
                window.Stage.GetComponent<Image>().StartCoroutine(coroutine);
            }
        }


        /// <summary>
        /// Coroutine that plays the specified sound at the specified time.
        /// If a time range is supplied, it will loop the sound.
        /// If a time range cycle value is supplied, it will repeat the sound.
        /// </summary>
        IEnumerator Schedule(ViewWindow window, CutsceneProperty time, Sound sound)
        {
            ++runningCoroutines;

            try
            {
                if (time.StartValue > 0)
                    yield return new WaitForSecondsRealtime(time.StartValue);

                GameObject go = new GameObject("Sound Emitter {" + time.StartValue + "}");
                go.transform.SetParent(window.Stage.transform); //stage gets automatically destroyed

                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = DaggerfallUnity.Instance.SoundReader.GetAudioClip(sound.SoundClip);
                source.spatialize = false;
                source.panStereo = 0.0f;
                source.pitch = 1.0f;
                source.volume = 1.0f * DaggerfallUnity.Settings.SoundVolume;

                float duration = time.EndValue - time.StartValue;
                float startTime = Time.unscaledTime;
                float endTime = startTime + duration;

                if (time.Cycles > 0 && duration > 0)
                {
                    float interval = duration / time.Cycles;

                    do
                    {
                        ModifyAudioProperties(source, sound, startTime, duration);
                        source.Play();
                        if (Time.unscaledTime + interval < endTime)
                            yield return new WaitForSecondsRealtime(interval);
                        else
                            yield return new WaitForSecondsRealtime(endTime - Time.unscaledTime + 0.03f);
                    }
                    while (Time.unscaledTime < endTime);
                }
                else
                {
                    if (duration > 0)
                        source.loop = true;

                    source.Play();

                    do
                    {
                        ModifyAudioProperties(source, sound, startTime, duration);
                        yield return null;
                    } while (Time.unscaledTime < endTime);

                    if (duration > 0)
                        source.Stop();
                }

            }
            finally
            {
                --runningCoroutines;
            }
        }


        /// <summary>
        /// Modulates audiosource properties
        /// </summary>
        void ModifyAudioProperties(AudioSource source, Sound sound, float startTime, float duration)
        {
            float elapsedTime = Time.unscaledTime - startTime;
            float lerpValue = 1;
            if (duration > 0)
                lerpValue = elapsedTime / duration;

            foreach (CutsceneProperty property in sound.Properties)
            {
                float newValue = Mathf.Lerp(property.StartValue, property.EndValue, lerpValue);

                if (property.Type == CutscenePropertyType.Balance)
                    //-100 is left, 100 is right, 0 is center
                    source.panStereo = newValue / 100f;
                else if (property.Type == CutscenePropertyType.Pitch)
                    //100 is normal pitch, >100 for higher pitch, <100 for lower pitch
                    source.pitch = newValue / 100f;
                else if (property.Type == CutscenePropertyType.Volume)
                    //0-100
                    source.volume = (newValue / 100f) * DaggerfallUnity.Settings.SoundVolume;
            }
        }



        /// <summary>
        /// Starts the coroutines that show captions at the bottom of the view window.
        /// </summary>
        void ScheduleCaptions(ViewWindow window)
        {
            foreach (Caption caption in captions)
            {
                CutsceneProperty time = caption.Properties.Get(CutscenePropertyType.Time);
                IEnumerator coroutine = Schedule(window, time, caption);
                window.Stage.GetComponent<Image>().StartCoroutine(coroutine);
            }
        }


        /// <summary>
        /// Coroutine that shows the caption at the specified time.
        /// </summary>
        IEnumerator Schedule(ViewWindow window, CutsceneProperty time, Caption caption)
        {
            ++runningCoroutines;

            try
            {
                if (time.StartValue > 0)
                    yield return new WaitForSecondsRealtime(time.StartValue);

                float duration = time.EndValue - time.StartValue;
                if (duration == 0)
                    duration = GetDuration() - time.StartValue;

                CutsceneProperty tint = caption.Properties.Get(CutscenePropertyType.Tint, true);

                window.ShowCaption(caption.Tokens, Time.unscaledTime + duration, tint);
            }
            finally
            {
                --runningCoroutines;
            }
        }


        /// <summary>
        /// Retrieves the model corresponding to the provided id/name.
        /// </summary>
        Model GetModel(string id)
        {
            if (HasModel(id))
                return models.Find(x => x.ID.Equals(id));
            else
                throw new Exception(string.Format("Unknown model ID '{0}'", id));
        }


        class Sound
        {
            readonly SoundClips soundClip;
            readonly CutsceneProperties properties;

            public Sound(SoundClips soundClip, CutsceneProperties properties)
            {
                this.soundClip = soundClip;
                this.properties = properties;
            }

            public SoundClips SoundClip { get { return soundClip; } }
            public CutsceneProperties Properties { get { return properties; } }
        }


        class Caption
        {
            readonly TextFile.Token[] tokens;
            readonly CutsceneProperties properties;

            public Caption(TextFile.Token[] tokens, CutsceneProperties properties)
            {
                this.tokens = tokens;
                this.properties = properties;
            }

            public TextFile.Token[] Tokens { get { return tokens; } }
            public CutsceneProperties Properties { get { return properties; } }
        }


        /// <summary>
        /// Class used to store per-model animation data
        /// </summary>
        class Model
        {
            readonly string id;
            readonly TextureSpecifier texture;
            readonly TiledTexture tiled;
            readonly ActorSpecifier actor;
            readonly List<CutsceneProperties> animations;

            public Model(string id)
            {
                this.id = id;
                animations = new List<CutsceneProperties>();
            }

            public Model(string id, TextureSpecifier texture) : this(id)
            {
                this.texture = texture;
            }

            public Model(string id, TiledTexture tiled) : this(id)
            {
                this.tiled = tiled;
            }

            public Model(string id, ActorSpecifier actor) : this(id)
            {
                this.actor = actor;
            }

            public string ID { get { return id; } }
            public List<CutsceneProperties> Animations { get { return animations; } }

            public void AddAnimation(CutsceneProperties properties)
            {
                animations.Add(properties);
            }

            public void SetTexture(ModelImage modelImage)
            {
                if (actor != null)
                {
                    Texture2D tex = Utility.GetPaperDoll(actor);
                    modelImage.SetTexture(tex, true);
                }
                else if (tiled != null)
                {
                    Texture2D tex = Utility.CreateTiledTexture(tiled);
                    modelImage.SetTexture(tex, false);
                }
                else if (texture.IsCustom)
                {
                    Texture2D tex = Utility.GetTexture(texture.Custom);
                    modelImage.SetTexture(tex, false);
                }
                else
                {
                    (int archive, int record, int frame) = texture.Archived;
                    modelImage.SetTexture(archive, record, frame);
                }
            }


        } //class Model



    } //class Clip



} //namespace
