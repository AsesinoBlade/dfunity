// Project:     Tempered Interiors for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: October 2022

using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TemperedInteriors
{
    public static class Filth
    {

        static readonly List<DFLocation.BuildingTypes> residenceTypes = new List<DFLocation.BuildingTypes>()
        {
            DFLocation.BuildingTypes.Tavern,
            DFLocation.BuildingTypes.House1, DFLocation.BuildingTypes.House2,
            DFLocation.BuildingTypes.House3, DFLocation.BuildingTypes.House4
        };



        /// <summary>
        /// Main entry point for adding filth (stains, shit, rats, etc)
        /// </summary>
        public static void Add()
        {
            byte quality = TemperedInteriorsMod.Instance.Quality;

            if (quality > 6)
                return;

            //Currently only adding filth to Inn's and houses
            DFLocation.BuildingTypes buildingType = TemperedInteriorsMod.Instance.BuildingType;
            if (!residenceTypes.Contains(buildingType))
                return;
            
            //potential pointsOfInterest for rats
            List<Vector3> pointsOfInterest = new List<Vector3>();

            List<Room> rooms = GetRooms(TemperedInteriorsMod.Instance.Doors);

            List<Vector3> itemLocations = AddItems(rooms, quality);
            pointsOfInterest.AddRange(itemLocations);

            AddRats(rooms, quality, pointsOfInterest);

            AddSkellies(rooms, quality);
        }


        /// <summary>
        /// Adds a static semi-transparent billboard to a surface, representing a stain.
        /// </summary>
        public static GameObject AddStain(Vector3 location, Vector3 facing, Transform parent)
        {
            GameObject go = new GameObject("Tempered Interiors Stain");
            go.transform.parent = parent;

            StaticBillboard billboard = go.AddComponent<StaticBillboard>();

            Vector2 size = new Vector2(Utility.Random(0.4f, 0.8f), Utility.Random(0.4f, 0.8f));
            billboard.SetMaterial(Textures.Stain.Get(), size);

            billboard.transform.forward = facing;

            if (Vector3.Angle(facing, Vector3.up) < 1)
                location = Utility.FindGround(location + facing * 0.5f);

            location += facing * 0.01f;
            billboard.transform.position = location;
            billboard.transform.Rotate(facing, Utility.Random(0f, 359f), Space.World);

            return go;
        }


        /// <summary>
        /// Potentially add food bits, stains, and shit to rooms
        /// </summary>
        static List<Vector3> AddItems(List<Room> rooms, byte quality)
        {
            //re-seeding random generator in case the other code introduces inconsistencies
            int seed = (int)Utility.GenerateHashValue(TemperedInteriorsMod.Instance.TransitionArgs.DaggerfallInterior, Vector3.zero);
            seed += DaggerfallUnity.Instance.WorldTime.Now.DayOfYear;
            Utility.Init(seed);

            List<Vector3> itemLocations = new List<Vector3>();

            foreach (Room room in rooms)
            {
                itemLocations.AddRange(AddBits(room, quality));
                itemLocations.AddRange(AddStains(room, quality));
                itemLocations.AddRange(AddPoop(room, quality));
            }

            return itemLocations;
        }


        /// <summary>
        /// Gets list of rooms, which are areas on either side of interior doors.
        /// </summary>
        static List<Room> GetRooms(List<GameObject> doors)
        {
            List<Room> rooms = new List<Room>();

            foreach (GameObject door in doors)
            {
                //Estimate dimensions of rooms on both sides of the door.
                //The simple algorithm will sometimes give bad results for certain geometries.

                Bounds bounds = GetBounds(door.transform.position, door.transform.forward);
                Room room = new Room(bounds);
                Room existing = FindRoom(rooms, room);
                if (existing == null)
                    rooms.Add(room);
                else
                    ++existing.doorCount;


                bounds = GetBounds(door.transform.position, -door.transform.forward);
                room = new Room(bounds);
                existing = FindRoom(rooms, room);
                if (existing == null)
                    rooms.Add(room);
                else
                    ++existing.doorCount;
            }

            return rooms;
        }


        /// <summary>
        /// Checks if the specified room has already been added to the list, returns it if so, null otherwise.
        /// </summary>
        static Room FindRoom(List<Room> rooms, Room room)
        {
            foreach (Room existing in rooms)
            {
                Vector3 path = existing.bounds.center - room.bounds.center;
                Vector3 direction = path.normalized;
                float distance = path.magnitude + 0.1f;

                if (!Physics.Raycast(room.bounds.center, direction, distance))
                    return existing;
            }

            return null;
        }


        /// <summary>
        /// Main entry point for adding filth (stains, shit, rats, etc)
        /// </summary>
        static List<Vector3> AddBits(Room room, byte quality)
        {
            List<Vector3> locations = new List<Vector3>();

            float area = room.bounds.size.x * room.bounds.size.z;
            if (quality > 6 || area < 18)
                return locations;

            Transform parent = GameObjectHelper.GetBestParent();

            bool scaleDown = !TemperedInteriorsMod.Instance.UsingHiResTextures;

            while (quality < Utility.Random(-2, 8))
            {
                GameObject go = new GameObject("Tempered Interiors FoodBit");
                go.transform.parent = parent;
                DaggerfallBillboard dfBillboard = go.AddComponent<DaggerfallBillboard>();

                Texture2D tex = Utility.GetStashedTexture(Textures.FoodBit, scaleDown);

                Vector2 size = new Vector2(Utility.Random(0.08f, 0.12f), Utility.Random(0.08f, 0.12f));

                dfBillboard.SetMaterial(tex, size);

                go.transform.position = DetermineItemLocation(room, dfBillboard);
                locations.Add(go.transform.position);
            }

            return locations;
        }


        /// <summary>
        /// Add a variable number of stains to a room
        /// </summary>
        static List<Vector3> AddStains(Room room, byte quality)
        {
            List<Vector3> locations = new List<Vector3>();

            Transform parent = GameObjectHelper.GetBestParent();

            while (quality < Utility.Random(-4, 10))
            {
                Vector3 location = DetermineItemLocation(room, null);
                AddStain(location, Vector3.up, parent);
                locations.Add(location);
            }

            return locations;
        }


        /// <summary>
        /// Adds a variable amount of shit to a room.
        /// </summary>
        static List<Vector3> AddPoop(Room room, byte quality)
        {
            List<Vector3> locations = new List<Vector3>();

            float area = room.bounds.size.x * room.bounds.size.z;
            if (area > 70)
                return locations;

            Transform parent = GameObjectHelper.GetBestParent();

            bool closet = area < 10;

            while (quality < (closet ? Utility.Random(-3, 7) : Utility.Random(-5, 5)))
            {
                GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(253, 21, parent);
                go.name = "Tempered Interiors Poop";
                go.transform.localScale *= 0.6f;
                DaggerfallBillboard billboard = go.GetComponent<DaggerfallBillboard>();
                go.transform.position = DetermineItemLocation(room, billboard);

                if (TemperedInteriorsMod.Instance.IsVisibleToProprietor(billboard))
                    GameObject.Destroy(go);
                else
                    locations.Add(go.transform.position);
            }

            return locations;
        }


        /// <summary>
        /// Adds a variable number of rats to a room.
        /// </summary>
        static void AddRats(List<Room> rooms, byte quality, List<Vector3> pointsOfInterest)
        {
            foreach (Room room in rooms)
            {
                float area = room.bounds.size.x * room.bounds.size.z;
                if (area < 15)
                    continue;

                while (quality < Utility.Random(-5, 6))
                {
                    Vector3 location = DetermineItemLocation(room, null) + Vector3.up * 0.1f;
                    Rats.AddRat(Utility.Random(quality - 3, 10) < 0 ? 2 : 1, location, pointsOfInterest);
                }
            }
        }


        /// <summary>
        /// Are there skeletons in the closet?
        /// </summary>
        static void AddSkellies(List<Room> rooms, byte quality)
        {
            foreach (Room room in rooms)
            {
                float area = room.bounds.size.x * room.bounds.size.z;
                if (area > 6.5 || room.doorCount > 1 || room.bounds.size.x < 1 || room.bounds.size.z < 1)
                    continue;
                
                uint hash = Utility.GenerateHashValue(TemperedInteriorsMod.Instance.TransitionArgs.DaggerfallInterior, room.bounds.center);

                if (hash % 12 == 0)
                {
                    Transform parent = GameObjectHelper.GetBestParent();
                    GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(306, 1, parent);
                    go.name = "Tempered Interiors Skeleton";
                    DaggerfallBillboard dfBillboard = go.GetComponent<DaggerfallBillboard>();

                    Vector3 location = Utility.FindGround(room.bounds.center);
                    location += Vector3.up * (dfBillboard.Summary.Size.y / 2);

                    go.transform.position = location;
                }
            }
        }


        /// <summary>
        /// Finds an appropriate place in the room to place an item.
        /// </summary>
        static Vector3 DetermineItemLocation(Room room, DaggerfallBillboard item)
        {
            Vector3 direction = new Vector3(Utility.Random(-1f, 1f), 0f, Utility.Random(-1f, 1f)).normalized;
            Vector3 location = room.bounds.center;

            if (Physics.Raycast(room.bounds.center, direction, out RaycastHit hitInfo, 50))
                location += direction * Utility.Random(0f, hitInfo.distance - 0.15f);

            location = Utility.FindGround(location);

            if (item != null)
                location += Vector3.up * (item.Summary.Size.y * item.transform.localScale.y / 2);
            else
                location += Vector3.up * 0.02f;

            return location;
        }


        /// <summary>
        /// Guesses the bounds (center position and size) of a room on one side of a door.
        /// </summary>
        static Bounds GetBounds(Vector3 doorPosition, Vector3 doorDirection)
        {
            Bounds bounds = new Bounds();

            //adjust position higher up to reduce possibility of hitting furniture and open doors
            Vector3 pos = doorPosition + doorDirection * 0.45f;
            pos = Utility.FindCeiling(pos) + Vector3.down * 0.1f;

            const float maxRange = 200f;

            if (!Physics.Raycast(pos, doorDirection, out RaycastHit hitInfo, maxRange))
                return bounds;

            float forwardDistance = hitInfo.distance;

            Vector3 centerForward = pos + doorDirection * (forwardDistance / 2);

            Vector3 acrossDirection = Vector3.Cross(doorDirection, Vector3.up).normalized;

            if (!Physics.Raycast(centerForward, acrossDirection, out hitInfo, maxRange))
                return bounds;

            Vector3 side = hitInfo.point;

            if (!Physics.Raycast(side, -acrossDirection, out hitInfo, maxRange))
                return bounds;

            float acrossDistance = hitInfo.distance;

            bounds.center = side - acrossDirection * (acrossDistance / 2);
            bounds.size = new Vector3(forwardDistance, 2f, acrossDistance);
            
            return bounds;
        }



        class Room
        {
            public Bounds bounds;
            public int doorCount;

            public Room(Bounds bounds)
            {
                this.bounds = bounds;
                doorCount = 1;
            }
        }



    } //class Filth

} //namespace
