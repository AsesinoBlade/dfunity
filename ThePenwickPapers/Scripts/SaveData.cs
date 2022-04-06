// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System;
using System.Collections.Generic;
using UnityEngine;


namespace ThePenwickPapers
{
    public class LandmarkLocation : IComparable<LandmarkLocation>
    {
        public string Name;
        public Vector3 Position;

        public LandmarkLocation(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }

        public int CompareTo(LandmarkLocation other)
        {
            if (other == null)
                return 1;
            else
                return this.Name.CompareTo(other.Name);
        }
    }

    public class SaveData
    {
        public Dictionary<string, List<LandmarkLocation>> Towns;
        public List<LandmarkLocation> DungeonLocations;
        public SaveData()
        {
            Towns = new Dictionary<string, List<LandmarkLocation>>();
            DungeonLocations = new List<LandmarkLocation>();
        }
    }


}
