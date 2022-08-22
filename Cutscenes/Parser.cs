// Project:         Cutscene animation for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: DunnyOfPenwick

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Questing;

namespace Cutscene
{
    /// <summary>
    /// This class processes commands, likely from a quest script, to create simple animations
    /// </summary>
    public class Parser
    {
        static readonly char[] whitespace = { ' ', '\t', '\n', '\r' };

        readonly int questID;
        readonly Quest parentQuest = null;
        readonly string[] script;

        Clip clip;
        float clipDuration;


        public int QuestID { get { return questID; } }
        public Quest ParentQuest { get { return parentQuest; } }



        /// <summary>
        /// Primarily intended for use by the Unity Editor tool for testing a single clip.
        /// </summary>
        /// <returns>The Clip, or null if there was an error creating the Clip</returns>
        public static Clip CreateClip(string[] script)
        {
            Parser parser = new Parser(script);

            parser.Evaluate();

            return parser.GetCutsceneClip();
        }


        /// <summary>
        /// Creates a Cutscene for the Quest system.
        /// </summary>
        public static Clip CreateClip(Quest parentQuest, int questID, string[] script)
        {
            Parser parser = new Parser(parentQuest, questID, script);

            parser.Evaluate();
            
            return parser.GetCutsceneClip();
        }


        /// <summary>
        /// Constructor for Quest system
        /// </summary>
        Parser(Quest parentQuest, int questID, string[] script) : this(script)
        {
            this.parentQuest = parentQuest;
            this.questID = questID;
        }


        /// <summary>
        /// Basic constructor for non-Quest use
        /// </summary>
        Parser(string[] script)
        {
            this.script = script;
        }



        /// <summary>
        /// Evaluates commands to build the cutscene Clip.
        /// </summary>
        /// <returns>true if a clip was created, false if there was a problem</returns>
        bool Evaluate()
        {
            if (clip != null)
            {
                return true;
            }

            clip = new Clip();

            List<string> delayed = new List<string>();
            List<int> delayedLineNum = new List<int>();

            clipDuration = 0;

            int lineNum = 0;

            try
            {
                foreach (string line in script)
                {
                    try
                    {
                        ++lineNum;
                        EvaluateCommand(line);
                    }
                    catch (DelayedEvaluationException)
                    {
                        //This happens when the source line contains a negative Time property value.
                        //Negative time values measure time backward from the end of the clip.
                        //These commands can't be evaluated until the entire running time is known.
                        delayedLineNum.Add(lineNum);
                        delayed.Add(line);
                    }
                }

                if (delayed.Count > 0)
                {
                    clipDuration = clip.GetDuration();
                    if (clipDuration <= 0)
                    {
                        throw new CutsceneException("Cutscene clip has no length.  You must specify at least one time range of fixed duration.");
                    }
                    for (int i = 0; i < delayed.Count; ++i)
                    {
                        lineNum = delayedLineNum[i];
                        EvaluateCommand(delayed[i]);
                    }

                }
            }
            catch (CutsceneException e)
            {
                Debug.LogErrorFormat("Error at cutscene script line {0}: {1}", lineNum, e.Message);
                clip = null;
            }

            return clip != null;
        }


        /// <summary>
        /// Gets the cutscene Clip.
        /// The value will be null if Evaluate() hasn't been called.
        /// </summary>
        Clip GetCutsceneClip()
        {
            return clip;
        }


        /// <summary>
        /// Evaluates a single line
        /// </summary>
        void EvaluateCommand(string line)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith("-"))
                return;

            line = CleanString(line);

            string[] words = line.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);

            Tokenizer tokens = new Tokenizer(words);

            string command = tokens.Next();

            switch (command.ToLower())
            {
                case "prop":
                    CreateProp(tokens);
                    break;
                case "actor":
                    CreateActor(tokens);
                    break;
                case "tile":
                    Tile(tokens);
                    break;
                case "change":
                    Change(tokens);
                    break;
                case "caption":
                    AddCaption(tokens);
                    break;
                case "sound":
                    PlaySound(tokens);
                    break;
                case "music":
                    PlayMusic(tokens);
                    break;
                default:
                    throw new CutsceneException(string.Format("Unknown command '{0}'", command));
            }
        }


        /// <summary>
        /// Remove any control characters
        /// </summary>
        /// <returns>Cleaned string</returns>
        string CleanString(string entry)
        {
            StringBuilder newEntry = new StringBuilder(entry.Length);

            foreach (char c in entry)
            {
                if (c == '\t')
                    newEntry.Append(' ');
                else if (!char.IsControl(c)) //ignore control characters
                    newEntry.Append(c);
            }

            return newEntry.ToString();
        }


        /// <summary>
        /// Registers a prop model for a specified texture.
        /// The texture can be a pair of numbers (archive:record), or a :FileName for a custom texture.
        /// example 1: prop bob 183:0
        /// example 2: prop bob :CustomPngFile
        /// </summary>
        void CreateProp(Tokenizer args)
        {
            string id = ParseName(args.Next()).ToLower();

            if (clip.HasModel(id))
                throw new CutsceneException(string.Format("Model ID '{0}' already exists.", id));

            TextureSpecifier texture = ParseTextureSpecifier(args.Next());
            clip.CreateModel(id, texture);

            try
            {
                CutsceneProperties properties = ParseProperties(args);
                ChangeModel(id, properties);
            }
            catch (DelayedEvaluationException)
            {
                throw new CutsceneException("Can't use negative time property when specifying a prop.");
            }

        }


        /// <summary>
        /// Registers an actor (paper doll) identifier.
        /// example 1: actor pc pc1
        /// example 2: actor john breton:male:2 
        /// </summary>
        void CreateActor(Tokenizer args)
        {
            string id = ParseName(args.Next()).ToLower();

            if (clip.HasModel(id))
                throw new CutsceneException(string.Format("Model ID '{0}' already exists.", id));

            string specifier = args.Next();
            string[] elements = specifier.Split(':');

            //zero or more equipment items, given as archive:record texture numbers
            List<(int, int, int)> equipment = new List<(int, int, int)>();
            while (args.HasMore)
                equipment.Add(ParseTuple(args.Next()));

            //TODO : currently all actors are just the basic PC paper doll
            ActorSpecifier actor = new ActorSpecifier(2, equipment);
            
            clip.CreateModel(id, actor);
        }


        /// <summary>
        /// Format: tile <id-name> <width> <height> <fill-texture> [optional additional textures]
        /// example 1: tile wall 6 1 3:5
        /// example 2: tile wall 6 1 3:5 3:6 0:0 0:0
        /// </summary>
        void Tile(Tokenizer args)
        {
            string id = ParseName(args.Next()).ToLower();

            if (clip.HasModel(id))
                throw new CutsceneException(string.Format("Model ID '{0}' already exists.", id));

            int width = (int)args.NextNumber();
            int height = (int)args.NextNumber();

            if (width < 1 || height < 1)
                throw new CutsceneException("Invalid tiling height and/or width value supplied");

            
            TextureSpecifier fillTexture = ParseTextureSpecifier(args.Next());

            List<TextureSpecifier> textures = new List<TextureSpecifier>();
            while (args.HasMore)
            {
                TextureSpecifier tex = ParseTextureSpecifier(args.Next());
                textures.Add(tex);
            }

            TiledTexture tiled = new TiledTexture(width, height, fillTexture, textures);

            clip.CreateModel(id, tiled);
        }


        /// <summary>
        /// Adds quest text specified by message ID to be shown at time specified by the time property
        /// </summary>
        void AddCaption(Tokenizer args)
        {
            int messageId = (int)args.NextNumber();

            TextFile.Token[] text;

            if (parentQuest != null)
            {
                Message message = parentQuest.GetMessage(messageId);
                if (message == null)
                    throw new CutsceneException("Uknown caption message key");

                text = message.GetTextTokens();
            }
            else
            {
                string placeHolder = string.Format("<Placeholder text for Quest Message {0}>", messageId);
                text = new TextFile.Token[] { TextFile.CreateTextToken(placeHolder) };
            }

            CutsceneProperties properties = ParseProperties(args);

            clip.AddCaption(text, properties);
        }



        /// <summary>
        /// Will attach property changes to the model.
        /// The included Time property will determine when changes are made while playing the Clip.
        /// If a Time property isn't supplied, the changes will occur at the start of the Clip.
        /// </summary>
        void Change(Tokenizer args)
        {
            string id = ParseName(args.Next()).ToLower();

            if (!clip.HasModel(id))
                throw new CutsceneException(string.Format("Model with ID of '{0}' doesn't exist.", id));

            CutsceneProperties properties = ParseProperties(args);

            ChangeModel(id, properties);
        }


        /// <summary>
        /// Plays sound (from the SoundClips enum) at the time specified by the Time property.
        /// If the time property is a range, the sound will loop
        /// If the time property includes a cycles value, the sound will be repeated
        /// </summary>
        void PlaySound(Tokenizer args)
        {
            string desiredSound = ParseName(args.Next());

            CutsceneProperties properties = ParseProperties(args);

            SoundClips sound = 0;
            bool found = false;

            foreach (string soundName in Enum.GetNames(typeof(SoundClips)))
            {
                if (soundName.Equals(desiredSound, StringComparison.OrdinalIgnoreCase))
                {
                    sound = (SoundClips)Enum.Parse(typeof(SoundClips), soundName);
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new CutsceneException(string.Format("SoundClips '{0}' is not a known sound clip", desiredSound));

            clip.AddSound(sound, properties);
        }


        /// <summary>
        /// Sets the song to be played during the clip, from the SongFiles enum.
        /// If music is never specified for the clip, the last song will continue playing.
        /// Specifying the song 'song_none' will cause music to be muted.
        /// </summary>
        void PlayMusic(Tokenizer args)
        {
            string desiredSong = ParseName(args.Next());

            SongFiles songFile = SongFiles.song_none;
            bool found = false;

            foreach (string songName in Enum.GetNames(typeof(SongFiles)))
            {
                if (songName.Equals(desiredSong, StringComparison.OrdinalIgnoreCase))
                {
                    songFile = (SongFiles)Enum.Parse(typeof(SongFiles), songName);
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new CutsceneException(string.Format("PlayMusic: SongFiles '{0}' is not a known song", desiredSong));

            clip.SetSong(songFile);
        }


        /// <summary>
        /// Changes a property of the model (prop, actor, tile) at the specified Time.
        /// If a Time range is specifed, the change will occur gradually (lerp) over that time.
        /// </summary>
        void ChangeModel(string id, CutsceneProperties properties)
        {
            if (properties.Count == 0)
                return;

            CutsceneProperty time = properties.Get(CutscenePropertyType.Time);

            //A property specifying a range of values needs a Time property that specifies the time range it occurs over
            foreach (CutsceneProperty property in properties)
                CheckMatchingTimeRange(time, property);

            clip.Change(id, properties);
        }



        /// <summary>
        /// Parses multiple consecutive property values.
        /// Properties are of the form propertyName:value or propertyName:startValue:endValue
        /// </summary>
        CutsceneProperties ParseProperties(Tokenizer args)
        {
            CutsceneProperties properties = new CutsceneProperties();
            while (args.HasMore)
            {
                CutsceneProperty property = ParseProperty(args.Next());
                properties.Add(property);
            }
            
            return properties;
        }


        /// <summary>
        /// Parses a single property value.
        /// Properties are of the form propertyName:value or propertyName:startValue:endValue
        /// The Time property can also have a third 'cycles' value
        /// </summary>
        CutsceneProperty ParseProperty(string entry)
        {
            string[] elements = entry.Split(':');

            int valueCount = elements.Length - 1;

            CutscenePropertyType type = CutscenePropertyType.X;
            bool found = false;
            string name = elements[0];

            foreach (string propertyName in Enum.GetNames(typeof(CutscenePropertyType)))
            {
                if (propertyName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    type = (CutscenePropertyType)Enum.Parse(typeof(CutscenePropertyType), propertyName);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (char.IsLetter(name[0]))
                    throw new CutsceneException(string.Format("Unknown property '{0}'", name));
                else
                    throw new CutsceneException(string.Format("Expecting property, found '{0}'", entry));
            }


            CutsceneProperty property = new CutsceneProperty(type);

            if (property.Type == CutscenePropertyType.Tint)
            {
                property.StartColor = ParseColor(elements[1]);
                property.EndColor = valueCount == 2 ? ParseColor(elements[2]) : property.StartColor;
            }
            else
            {
                property.StartValue = valueCount > 0 ? ParseNumber(elements[1]) : 0;
                property.EndValue = valueCount > 1 ? ParseNumber(elements[2]) : property.StartValue;
                property.Cycles = valueCount > 2 ? ParseNumber(elements[3]) : 0;
            }


            if (property.Type == CutscenePropertyType.Time)
            {
                if (clipDuration <= 0 && (IsNegative(property.StartValue) || IsNegative(property.EndValue)))
                {
                    //delay this command evaluation until later
                    throw new DelayedEvaluationException();
                }
                else if (clipDuration > 0)
                {
                    property.StartValue = IsNegative(property.StartValue) ? clipDuration + property.StartValue : property.StartValue;
                    property.EndValue = IsNegative(property.EndValue) ? clipDuration + property.EndValue : property.EndValue;
                }

                CheckValidTimeRange(property);
            }

            return property;
        }


        /// <summary>
        /// Determines if value is negative,  with special check for negative zero (yes, -0 is a number).
        /// </summary>
        bool IsNegative(float value)
        {
            //need special check for negative zero
            if (value < 0)
                return true;
            else if (float.IsNegativeInfinity(1.0f / value))
                return true; // negative zero
            else
                return false;
        }


        /// <summary>
        /// Checks that a property that has a range of values has a Time property that also has a range of values.
        /// </summary>
        void CheckMatchingTimeRange(CutsceneProperty time, CutsceneProperty property)
        {
            if (property.IsRange() && !time.IsRange())
            {
                string msg = string.Format("Value changes for property '{0}' require a time range.", property.Type);
                throw new CutsceneException(msg);
            }
        }


        /// <summary>
        /// Checks if Time property is valid (startTime <= endTime).
        /// </summary>
        void CheckValidTimeRange(CutsceneProperty property)
        {
            if (property.StartValue > property.EndValue)
                throw new CutsceneException("Invalid time range, start time can't be greater than end time.");

            if (property.StartValue < 0 || property.EndValue < 0)
                throw new CutsceneException("Invalid time range, resulting time value is less than zero.");
        }


        /// <summary>
        /// Parses a name entry; an entry starting with an alphabetical character.
        /// </summary>
        string ParseName(string entry)
        {
            if (char.IsLetter(entry[0]))
                return entry;
            else
                throw new CutsceneException("Expecting an ID/Property argument starting with an alphabetical letter.");
        }


        /// <summary>
        /// Parses a numerical entry as a float.
        /// </summary>
        float ParseNumber(string entry)
        {
            try
            {
                //float.Parse() doesn't correctly parse negative zero for some reason
                float value = float.Parse(entry);
                if (value == 0 && entry.StartsWith("-"))
                    value = -0.0f;

                return value;
            }
            catch
            {
                throw new CutsceneException(string.Format("Expecting a number, got '{0}'.", entry));
            }
        }



        /// <summary>
        /// Parses a tuple of the form 'archive:record' or 'archive:record:frame'.
        /// If value3 is not specified, -1 is assumed.
        /// </summary>
        (int, int, int) ParseTuple(string entry)
        {
            try
            {
                string[] elements = entry.Split(':');

                int archive = int.Parse(elements[0]);
                int record = int.Parse(elements[1]);
                int frame = elements.Length > 2 ? int.Parse(elements[2]) : -1;
                return (archive, record, frame);
            }
            catch
            {
                throw new CutsceneException(string.Format("Expecting 'archive:record' or 'archive:record:frame', got '{0}'.", entry));
            }
        }



        /// <summary>
        /// Checks if entry is a tuple of the form 'archive:record' or 'archive:record:frame'.
        /// </summary>
        bool IsTuple(string entry)
        {
            try
            {
                ParseTuple(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }



        /// <summary>
        /// Parse an rgba value, where each value is in the range 0-100.<br />
        /// </summary>
        Color ParseColor(string entry)
        {
            float[] values = new float[4] { -1, -1, -1, -1 };
            int index = -1;

            entry = entry.ToLower();

            if (entry.Equals("white"))
                return Color.white;
            else if (entry.Equals("black"))
                return Color.black;

            foreach (char c in entry.ToLower().ToCharArray())
            {
                int newIndex = "rgba".IndexOf(c);
                if (newIndex >= 0)
                {
                    index = newIndex;
                    values[index] = 0;
                }
                else if (char.IsDigit(c) && index != -1)
                {
                    values[index] = values[index] * 10 + (int)char.GetNumericValue(c);
                }
                else
                {
                    throw new CutsceneException("Invalid color value.  Must be a r0g0b0a0 value or 'white'/'black'.  No decimals.");
                }
            }

            bool colorSpecified = values[0] != -1 || values[1] != -1 || values[2] != -1;
            float defaultValue = colorSpecified ? 0 : -1;

            float red = values[0] == -1 ? defaultValue : (values[0] / 100f);
            float green = values[1] == -1 ? defaultValue : (values[1] / 100f);
            float blue = values[2] == -1 ? defaultValue : (values[2] / 100f);
            float alpha = values[3] == -1 ? -1 : (values[3] / 100f);

            return new Color(red, green, blue, alpha);
        }



        /// <summary>
        /// Tries to get the specified texture specifier, check existence of...
        /// Will throw an exception if not found.
        /// </summary>
        TextureSpecifier ParseTextureSpecifier(string entry)
        {
            TextureSpecifier texSpecifier;

            if (IsTuple(entry))
            {
                texSpecifier = new TextureSpecifier(ParseTuple(entry));
            }
            else
            {
                if (!entry.StartsWith(":"))
                    throw new CutsceneException("Expecting an 'archive:record', 'archive:record:frame', or a ':Custom' texture value");

                texSpecifier = new TextureSpecifier(entry.Substring(1));
            }

            return texSpecifier;
        }



        class Tokenizer
        {
            readonly Queue<string> queue;

            public Tokenizer(string[] words)
            {
                queue = new Queue<string>(words);
            }

            public bool HasMore { get { return queue.Count > 0; } }

            public string Peek()
            {
                CheckHasMore();
                return queue.Peek();
            }

            public string Next()
            {
                CheckHasMore();
                return queue.Dequeue();
            }

            public float NextNumber()
            {
                string entry = Next();
                try
                {
                    return float.Parse(entry);
                }
                catch
                {
                    throw new CutsceneException(string.Format("Expecting a number, got '{0}'.", entry));
                }
            }

            void CheckHasMore()
            {
                if (!HasMore)
                    throw new CutsceneException("Expecting an additional argument.");
            }

        }



        /// <summary>
        /// This Exception causes current command evaluation to be delayed
        /// </summary>
        class DelayedEvaluationException : Exception
        {
        }




    } //class Parser




} //namespace



