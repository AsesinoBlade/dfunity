// Project:         Cutscene animations for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using UnityEngine;
using UnityEditor;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;



namespace Cutscene
{
    /// <summary>
    /// CutsceneTesterWindow for building/testing cutscene animation sequences
    /// </summary>
    public class TesterWindow: EditorWindow
    {
        const string windowTitle = "Cutscene Tester";
        const string menuPath = "Daggerfall Tools/Cutscene Tester";

        string srcText = "\n\n\n";
        Vector2 scrollPosition;
        ViewWindow cutsceneWindow;
        SoundClips clip;
        SongFiles song;

        [MenuItem(menuPath)]
        static void Init()
        {
            TesterWindow window = (TesterWindow)EditorWindow.GetWindow(typeof(TesterWindow));
            window.titleContent = new GUIContent(windowTitle);
        }

        void OnGUI()
        {
            if (!IsReady())
            {
                EditorGUILayout.HelpBox("DaggerfallUnity instance not ready.", MessageType.Info);
                return;
            }


            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Script: ", GUI.skin.FindStyle("BoldLabel"));

            EditorGUILayout.BeginVertical(GUILayout.MinHeight(200), GUILayout.MaxHeight(200));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUIStyle style = new GUIStyle(GUI.skin.textArea)
            {
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                //font = Font.CreateDynamicFontFromOSFont("Courier New", 16);
                fontSize = 16
            };

            srcText = GUILayout.TextArea(srcText, style);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();


            EditorGUILayout.Space();

            //GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));

            if (GameManager.HasInstance && GameManager.Instance.IsPlayingGame() && cutsceneWindow == null)
            {
                if (GUILayout.Button("Run"))
                {
                    // Remove focus if cleared
                    ParseAndRun();
                }
            }
            else if (cutsceneWindow != null)
            {
                GUILayout.TextField("Playing...");
            }
            else
            {
                GUILayout.TextField("Start game to test");
            }

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
            clip = (SoundClips)EditorGUILayout.EnumPopup(new GUIContent("Sounds"), clip);
            if (DaggerfallUI.Instance)
            {
                if (GUILayout.Button("Play Sound"))
                    DaggerfallUI.Instance.PlayOneShot(clip);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
            song = (SongFiles)EditorGUILayout.EnumPopup(new GUIContent("Music"), song);
            if (GUILayout.Button("Play Song"))
            {
                GameObject go = GameObject.Find("SongPlayer");
                if (go)
                {
                    DaggerfallSongPlayer songPlayer = go.GetComponent<DaggerfallSongPlayer>();
                    songPlayer.Play(song);
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Script Command Reference: ", GUI.skin.FindStyle("BoldLabel"));

            //GUILayout.EndHorizontal();

            if (cutsceneWindow != null && !cutsceneWindow.IsPlaying)
            {
                cutsceneWindow = null;
                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack(2.0f);
            }

        }


        void ParseAndRun()
        {
            string[] script = srcText.Split('\n');
            Clip clip = Parser.CreateClip(script);
            if (clip != null)
            {
                cutsceneWindow = new ViewWindow(DaggerfallUI.UIManager, clip);
                DaggerfallUI.Instance.UserInterfaceManager.PushWindow(cutsceneWindow);
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
            }

        }


        void Update()
        {
            if (cutsceneWindow != null && !cutsceneWindow.IsPlaying)
            {
                Repaint();
            }
        }


        bool IsReady()
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

            if (!dfUnity.IsReady || string.IsNullOrEmpty(dfUnity.Arena2Path))
                return false;

            return true;
        }



    } //class TesterWindow


} //namespace