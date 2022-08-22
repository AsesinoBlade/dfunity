// Project:         Cutscene animation for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;

namespace Cutscene
{
    public class ViewWindow : DaggerfallBaseWindow
    {
        readonly List<Clip> clips;
        DaggerfallSongPlayer songPlayer;
        GameObject theater;
        GameObject stageLight;
        GameObject stage;
        Panel lowerBar;
        MultiFormatTextLabel captionLabel;
        float endCaptionTime;
        float xScaler;
        float xOffset;
        float yScaler;
        float yOffset;
        bool isPlaying;
        bool changedMusic;
        bool skip;


        public ViewWindow(IUserInterfaceManager uiManager, Clip clip)
            : base(uiManager)
        {
            clips = new List<Clip>();

            if (clip != null)
                clips.Add(clip);
        }


        public ViewWindow(IUserInterfaceManager uiManager, List<Clip> clips)
            : base(uiManager)
        {
            this.clips = new List<Clip>();

            if (clips != null)
                this.clips.AddRange(clips);
        }


        public GameObject Stage { get { return stage; } }
        public GameObject StageLight { get { return stageLight; } }
        public float XScaler { get { return xScaler; } }
        public float XOffset { get { return xOffset; } }
        public float YScaler { get { return yScaler; } }
        public float YOffset { get { return yOffset; } }
        public bool IsPlaying { get { return isPlaying; } }



        protected override void Setup()
        {
            ParentPanel.BackgroundColor = Color.clear;
            NativePanel.BackgroundColor = Color.clear;

            NativePanel.OnMouseClick += CutsceneWindow_OnMouseClick;
            NativePanel.OnKeyboardEvent += CutsceneWindow_OnKeyboardEvent;
            
            Panel upperBar = new Panel
            {
                BackgroundColor = Color.black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Size = new Vector2(ParentPanel.Rectangle.width, ParentPanel.Rectangle.height / 12),
                VerticalAlignment = VerticalAlignment.Top
            };
            ParentPanel.Components.Add(upperBar);

            lowerBar = new Panel
            {
                BackgroundColor = Color.black,
                Size = new Vector2(ParentPanel.Rectangle.width, ParentPanel.Rectangle.height / 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ParentPanel.Components.Add(lowerBar);
            
            captionLabel = new MultiFormatTextLabel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                WrapText = true,
                WrapWords = true,
                MaxTextWidth = lowerBar.InteriorWidth,
                TextScale = 4
            };
            lowerBar.Components.Add(captionLabel);


            //get the song player, for music
            GameObject go = GameObject.Find("SongPlayer");
            songPlayer = go.GetComponent<DaggerfallSongPlayer>();

            IsSetup = true;

            PlaySequence();
        }


        public override void Update()
        {
            base.Update();

            InputManager.Instance.CursorVisible = true;

            if (endCaptionTime > 0 && Time.unscaledTime > endCaptionTime)
            {
                endCaptionTime = 0;
                captionLabel.SetText(new TextFile.Token[0]);
            }
        }


        public override void OnPush()
        {
            base.OnPush();

            isPlaying = true;
        }


        public override void OnPop()
        {
            base.OnPop();

            isPlaying = false;

            GameObject.Destroy(theater);
        }


        /// <summary>
        /// Attaches the model to the stage transform parent
        /// </summary>
        public void AddModelObjectToStage(GameObject modelObject)
        {
            RectTransform rectTransform = modelObject.GetComponent<RectTransform>();
            rectTransform.SetParent(stage.transform);
        }


        /// <summary>
        /// Shows caption text/tokens in the lower bar panel.  Removes it after endTime arrives.
        /// </summary>
        public void ShowCaption(TextFile.Token[] tokens, float endTime, CutsceneProperty tint)
        {
            captionLabel.ShadowColor = Color.black;
            captionLabel.TextColor = tint == null ? DaggerfallUI.DaggerfallDefaultTextColor : tint.StartColor;
            captionLabel.SetText(tokens);
            endCaptionTime = endTime;
        }



        /// <summary>
        /// Plays one or more clips in sequence using a coroutine.
        /// The caller can call the IsPlaying() method to determine completion.
        /// </summary>
        void PlaySequence()
        {
            InitTheater();

            stageLight.GetComponent<Image>().StartCoroutine(Play());
        }


        /// <summary>
        /// Creates the 'theater' object and stagelight.
        /// The theater is the object that the stage and stagelight are attached to.
        /// The theater and stagelight persist for the entire clip sequence.
        /// The stage object will be created and destroyed for each clip.
        /// </summary>
        void InitTheater()
        {
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();

            if (!theater)
            {
                //theater is a GameObject used for organizing the stageLight and stage objects.
                //stageLight is a transparent panel in front of the 'stage'.
                theater = new GameObject("theater");
                theater.transform.SetParent(canvas.transform);
                RectTransform rectTransform = theater.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;

                stageLight = new GameObject("stageLight");
                rectTransform = stageLight.AddComponent<RectTransform>();
                stageLight.AddComponent<Image>();
                stageLight.transform.SetParent(theater.transform);

                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;

            }

            theater.SetActive(true);
            stageLight.SetActive(true);
        }



        /// <summary>
        /// Coroutine used to play a List of clips and clean up when done
        /// </summary>
        IEnumerator Play()
        {
            try
            {
                foreach (Clip clip in clips)
                {
                    skip = false;

                    InitStage();

                    if (clip.HasMusic)
                    {
                        changedMusic = true;

                        if (clip.Song == SongFiles.song_none)
                        {
                            songPlayer.AudioSource.mute = true;
                        }
                        else
                        {
                            songPlayer.AudioSource.mute = false;
                            songPlayer.Play(clip.Song);
                        }
                    }

                    clip.Play(this);

                    while (clip.IsPlaying() && !skip)
                        yield return null;

                    //wait for any lingering captions to time out
                    while (Time.unscaledTime < endCaptionTime && !skip)
                        yield return null;

                    //destroy the stage after every clip
                    GameObject.Destroy(stage);
                }
            }
            finally
            {
                songPlayer.AudioSource.mute = false;

                if (changedMusic)
                    songPlayer.Stop(); //resets music

                PopWindow();
            }

        }



        /// <summary>
        /// Creates the stage, the object to which models will be attached.
        /// The stage is created before a clip is played, and should be destroyed after it is finished.
        /// </summary>
        void InitStage()
        {

            //reset stageLight properties in case a previous clip altered them
            stageLight.transform.localPosition = Vector3.zero;
            stageLight.transform.localRotation = Quaternion.identity;
            stageLight.GetComponent<Image>().color = Color.clear;

            RectTransform rectTransform = stageLight.GetComponent<RectTransform>();

            //We want a simplified 100x100 grid positioning, with the origin in lower left corner of the screen.
            xScaler = rectTransform.rect.width / 100f;
            xOffset = -rectTransform.rect.width / 2;
            yScaler = rectTransform.rect.height / 100f;
            yOffset = -rectTransform.rect.height / 2;

            //'stage' holds all the models/sprites and gets recreated for every clip
            if (stage)
                GameObject.Destroy(stage); //just in case previous stage wasn't disposed of

            stage = new GameObject("stage");
            rectTransform = stage.AddComponent<RectTransform>();
            Image image = stage.AddComponent<Image>();
            image.color = Color.black;

            stage.transform.SetParent(theater.transform);
            stage.transform.SetAsFirstSibling(); //put stage object behind stageLight object

            //stage is 3x3 screens in size
            rectTransform.anchorMin = new Vector2(-1, -1);
            rectTransform.anchorMax = new Vector2(2, 2);
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;

        }


        void SkipClip()
        {
            endCaptionTime = 0;
            skip = true;
        }


        void CutsceneWindow_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            SkipClip();
        }


        void CutsceneWindow_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyUp)
            {
                SkipClip();
            }
        }

        


    } //class ViewWindow



} //namespace


