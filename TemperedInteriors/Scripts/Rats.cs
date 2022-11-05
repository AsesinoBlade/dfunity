// Project:     Rats! for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: July 2022

using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Utility;


namespace TemperedInteriors
{

    public static class Rats
    {
        public static readonly string RatObjectName = "Tempered Interiors Rat Vermin";

        static Rats()
        {
            EnemyDeath.OnEnemyDeath += EnemyDeath_OnDeathHandler; //corpse handling
        }


        /// <summary>
        /// Adds a variable-sized passive rat to the area.
        /// </summary>
        /// <param name="size">From 1 to 3, 1 being mouse-size, 3 being cat-size.</param>
        /// <param name="location">Initial location of our sqeaking friend.</param>
        /// <param name="pointsOfInterest">Locations the rat may occasionally visit (food on floor etc).</param>
        public static DaggerfallEntityBehaviour AddRat(int size, Vector3 location, List<Vector3> pointsOfInterest = null)
        {
            size = Mathf.Clamp(size, 1, 3);

            DaggerfallEntityBehaviour rat = CreateRat(size, location);

            RatLogic ratLogic = rat.gameObject.AddComponent<RatLogic>();
            ratLogic.SetPointsOfInterest(pointsOfInterest);

            return rat;
        }


        /// <summary>
        /// Instantiates a rat and initializes its properties to match size.
        /// </summary>
        static DaggerfallEntityBehaviour CreateRat(int size, Vector3 location)
        {
            MobileTypes mobileType = MobileTypes.Rat;

            string displayName = RatObjectName;
            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);
            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            setupEnemy.ApplyEnemySettings(mobileType, MobileReactions.Passive, MobileGender.Male, 0, false);

            MobileUnit mobileUnit = setupEnemy.GetMobileBillboardChild();

            MobileEnemy mobileEnemy = mobileUnit.Enemy; //struct copy
            mobileEnemy.MinDamage = 1;
            mobileEnemy.MaxDamage = size;
            mobileEnemy.Weight = 1;
            mobileEnemy.MinHealth = size;
            mobileEnemy.MaxHealth = size * 2;

            //set new/altered MobileEnemy to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Passive, 0);

            DaggerfallEntityBehaviour rat = go.GetComponent<DaggerfallEntityBehaviour>();

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            EnemyEntity entity = rat.Entity as EnemyEntity;
            entity.SetEnemyCareer(mobileEnemy, rat.EntityType);

            //adjust visual rat size
            float scale = 0.2f * size;
            rat.transform.localScale = new Vector3(scale, scale, scale);

            CharacterController controller = rat.GetComponent<CharacterController>();
            controller.height = 0.65f * size;
            GameObjectHelper.AlignControllerToGround(controller);

            //modify rat audio characteristics to match smaller size
            DaggerfallAudioSource dfAudio = rat.GetComponent<DaggerfallAudioSource>();
            dfAudio.AudioSource.pitch += (5f - size) / 4f;
            dfAudio.AudioSource.volume /= 5f;

            go.SetActive(true);

            return rat;
        }


        /// <summary>
        /// Called when a creature dies, after its corpse is created. Makes adjustments to smaller rat corpses.
        /// </summary>
        static void EnemyDeath_OnDeathHandler(object sender, System.EventArgs args)
        {
            if (sender == null)
                return;

            EnemyDeath enemyDeath = (EnemyDeath)sender;

            if (enemyDeath.name.Equals(RatObjectName))
            {
                DaggerfallLoot corpse = enemyDeath.GetComponent<DaggerfallEntityBehaviour>().CorpseLootContainer;
                if (corpse != null)
                {
                    corpse.transform.localScale = enemyDeath.transform.localScale;
                    corpse.LoadID = 0; //prevent huge rat corpse from appearing on save/reload
                    Vector3 position = enemyDeath.GetComponent<EnemyMotor>().FindGroundPosition();
                    float radius = enemyDeath.GetComponent<CharacterController>().radius * corpse.transform.localScale.x;
                    corpse.transform.position = position + Vector3.up * radius;
                }
            }

        }

    } //class Rats



    class RatLogic : MonoBehaviour
    {
        List<Vector3> pointsOfInterest = new List<Vector3>();
        Vector3 destination;
        EnemyMotor motor;
        EnemySenses senses;
        CharacterController controller;
        float moveSpeed;
        float pawsTime;
        float lastChoiceTime;


        /// <summary>
        /// Supplies the rat with a list of points of interest.
        /// </summary>
        public void SetPointsOfInterest(List<Vector3> pointsOfInterest)
        {
            this.pointsOfInterest = pointsOfInterest ?? new List<Vector3>();
        }


        void Start()
        {
            motor = GetComponent<EnemyMotor>();
            senses = GetComponent<EnemySenses>();
            controller = GetComponent<CharacterController>();

            moveSpeed = 100f * MeshReader.GlobalScale; //moves slower than bigger brothers
        }


        void Update()
        {
            if (GameManager.IsGamePaused)
                return;

            if (motor.IsHostile)
            {
                //who pissed off Mister Squeakers?
                destination = Vector3.zero;
                return;
            }

            if (Time.time < pawsTime)
                return; //pausing, thinking rat thoughts

            if (destination == Vector3.zero)
                ChooseDestination();
            else
                Move();
        }


        /// <summary>
        /// Moves the rat an increment towards its destination.
        /// </summary>
        void Move()
        {
            if (Vector3.Distance(transform.position, destination) <= controller.radius + 0.03f)
            {
                destination = Vector3.zero;
                pawsTime = Time.time + Random.Range(0.0f, 4f);
            }
            else
            {
                Vector3 direction = (destination - transform.position).normalized;

                if (!senses.TargetIsWithinYawAngle(5.625f, destination))
                    transform.forward = Vector3.RotateTowards(transform.forward, direction, 20f * Mathf.Deg2Rad, 0.0f);

                Vector3 motion = direction * moveSpeed;
                controller.Move(motion * Time.deltaTime);
            }

            //extra bit to keep rat from getting stuck for some reason
            if (Time.time > lastChoiceTime + 6.0f)
                destination = Vector3.zero;
        }


        /// <summary>
        /// Rat chooses a desired destination.
        /// </summary>
        void ChooseDestination()
        {
            float distance;

            lastChoiceTime = Time.time;

            foreach (Vector3 point in pointsOfInterest)
            {
                distance = Vector3.Distance(transform.position, point);
                if (distance > 0.3f && distance < 6 && Dice100.SuccessRoll(13) && CanReach(point, distance))
                {
                    destination = point;
                    return;
                }
            }

            //no interesting points-of-interest, just explore
            float x = transform.position.x + Random.Range(-2f, 2f);
            float z = transform.position.z + Random.Range(-2f, 2f);
            Vector3 location = new Vector3(x, transform.position.y, z);
            distance = Vector3.Distance(transform.position, location);
            if (distance > 0.3f && CanReach(location, distance))
                destination = location;
        }


        /// <summary>
        /// Determines if rat can reach the specified location in a straight move, checks for obstacles.
        /// </summary>
        bool CanReach(Vector3 location, float distance)
        {
            float heightDiff = Mathf.Abs(transform.position.y - location.y);
            if (heightDiff > controller.radius + 0.05f)
                return false;

            //adjust location height to rat
            location = new Vector3(location.x, transform.position.y, location.z);

            //Don't try to go down stairs and other drop-offs
            if (!Physics.Raycast(location, Vector3.down, controller.radius + 0.05f))
                return false;

            Vector3 direction = (location - transform.position).normalized;

            return !Physics.SphereCast(transform.position, controller.radius + 0.03f, direction, out RaycastHit hit, distance);
        }


    } //class RatLogic



} //namespace
