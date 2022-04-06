using System.Collections;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility;

namespace ThePenwickPapers
{
    public class PenwickMinion : MonoBehaviour
    {
        private const string PenwickMinionPrefix = "Penwick Minion";
        private const string PenwickFollowerPrefix = "Penwick Follower";
        private const string PenwickFormerMinionPrefix = "Former Penwick Minion";
        private const float pushActivateDistance = 2.5f;
        private const float followTriggerDistance = 2.5f;

        private static bool autoTeleportMinions;

        private bool isFollower;
        private bool isBeingPushed;
        private DaggerfallEntityBehaviour proxyTarget; //invisible target used to control minion movement
        private Vector3 lastSeenPlayerPosition;



        /// <summary>
        /// This is called when the game load event occurs.
        /// Searches current location for any minion creatures and initializes their state.
        /// </summary>
        public static void InitializeOnLoad()
        {
            DaggerfallEntityBehaviour[] creatures = Object.FindObjectsOfType<DaggerfallEntityBehaviour>();

            foreach (DaggerfallEntityBehaviour creature in creatures)
            {
                if (creature.name.StartsWith(PenwickMinionPrefix))
                {
                    PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                    minion.Initialize(false);
                }
                else if (creature.name.StartsWith(PenwickFollowerPrefix))
                {
                    PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                    minion.Initialize(true);
                }
            }
        }


        /// <summary>
        /// Sets flag that indicates whether minions should be teleported behind player when they fall behind.
        /// </summary>
        public static void SetAutoTeleportMinions(bool teleport)
        {
            autoTeleportMinions = teleport;
        }


        /// <summary>
        /// Returns an array of all the PC minions in the current location.
        /// </summary>
        public static PenwickMinion[] GetMinions()
        {
            return GameObject.FindObjectsOfType<PenwickMinion>();
        }


        /// <summary>
        /// Applies minion status to the specified ally creature.
        /// </summary>
        public static void AddNewMinion(DaggerfallEntityBehaviour creature)
        {
            if (creature.Entity.Team == MobileTeams.PlayerAlly)
            {
                PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                minion.Initialize(CanAddFollower());
            }
        }


        /// <summary>
        /// Top level method for controlling minion movement.
        /// This is probably called from an Update() method.
        /// </summary>
        public static void GuideMinions()
        {
            foreach (PenwickMinion minion in GetMinions())
            {
                //Remove minion status for any minions that have turned on the player
                if (minion.GetComponent<DaggerfallEntityBehaviour>().Entity.Team != MobileTeams.PlayerAlly)
                {
                    minion.SetMinionObjectName();
                    GameObject.Destroy(minion); //remove minion component of GameObject
                    continue;
                }

                //perform movement and other minion activities
                minion.Guide();
            }
        }


        /// <summary>
        /// Used after player uses Landmark Journal, or after a long rest
        /// </summary>
        public static void RepositionFollowers()
        {
            IEnumerator coroutine = RepositionCoroutine();
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Restore health and magicka of some minions after long rest (6 hours or so).
        /// Undead/atronach minions can't restore health by resting.
        /// This exists for other minion types that may be added in the future.
        /// </summary>
        public static void Rest()
        {
            PenwickMinion[] minions = GetMinions();
            foreach (PenwickMinion minion in minions)
            {
                DaggerfallEntityBehaviour creature = minion.GetComponent<DaggerfallEntityBehaviour>();
                EnemyEntity creatureEntity = creature.Entity as EnemyEntity;
                MobileEnemy mobileEnemy = creatureEntity.MobileEnemy;

                creatureEntity.CurrentMagicka = creatureEntity.MaxMagicka;
                creatureEntity.CurrentFatigue = creatureEntity.MaxFatigue;

                //constructs and undead don't heal on their own
                if (mobileEnemy.Affinity != MobileAffinity.Undead && mobileEnemy.Affinity != MobileAffinity.Golem)
                {
                    creatureEntity.CurrentHealth = creatureEntity.MaxHealth;
                }
            }
        }


        /// <summary>
        /// Coroutine to reposition following minions near the player.
        /// </summary>
        private static IEnumerator RepositionCoroutine()
        {
            Vector3 playerPos = GameManager.Instance.PlayerController.transform.position;

            foreach (PenwickMinion minion in GetMinions())
            {
                if (!minion.isFollower)
                {
                    continue;
                }

                DaggerfallEntityBehaviour follower = minion.GetComponent<DaggerfallEntityBehaviour>();

                Vector3 followerPos = follower.transform.position;
                if (Vector3.Distance(playerPos, followerPos) < 6)
                {
                    continue; //close enough
                }

                //added yield delay to prevent position overlap
                yield return new WaitForSeconds(0.03f);

                //try to find appropriate nearby spot for minion
                for (int i = 0; i < 20; ++i)
                {
                    float x = playerPos.x + Random.Range(-3.0f, 3.0f);
                    float y = playerPos.y + 0.3f;
                    float z = playerPos.z + Random.Range(-3.0f, 3.0f);
                    Vector3 pos = new Vector3(x, y, z);

                    //need a floor beneath
                    if (!Physics.Raycast(pos, Vector3.down, 4.0f))
                        continue;

                    Vector3 top = pos + Vector3.up * 0.4f;
                    Vector3 bottom = pos - Vector3.up * 0.4f;
                    float radius = 0.4f; //radius*2 included in height
                    if (!Physics.CheckCapsule(top, bottom, radius))
                    {
                        follower.transform.position = pos;
                        break;
                    }
                }
                //if new position wasn't set, the follower will be stuck at original location
            }
        }


        /// <summary>
        /// Determines if PC can add another follower minion.  Determined by the Willpower stat.
        /// </summary>
        private static bool CanAddFollower()
        {
            int willpower = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower);

            int followerCount = 0;
            foreach (PenwickMinion minion in GetMinions())
            {
                followerCount += minion.isFollower ? 1 : 0;
            }

            //allow one follower for every 30 willpower
            return followerCount < willpower / 30;
        }


        /// <summary>
        /// Check if any terrain is between creature and destination
        /// </summary>
        private static bool HasPathTo(DaggerfallEntityBehaviour creature, Vector3 location)
        {
            Vector3 creaturePos = creature.transform.position;

            float distance = Vector3.Distance(location, creaturePos);

            Vector3 direction = (location - creaturePos).normalized;

            int layerMask = 1; //just looking for terrain hits

            RaycastHit hit;
            if (Physics.SphereCast(creature.transform.position, 0.35f, direction, out hit, distance, layerMask))
            {
                //if it's a door, we ignore it
                return (hit.collider.GetComponent<DaggerfallActionDoor>() != null);
            }

            return true;
        }


        /// <summary>
        /// Handles player activation when targeting minions (follow, stay, push).
        /// Depends on current mode (talk, grab, etc)
        /// </summary>
        public bool Activate(float distance)
        {
            if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Talk)
            {
                if (isFollower)
                {
                    //stop following, stay
                    isFollower = false;
                    GetComponent<EnemySenses>().Target = null;
                    SetMinionObjectName();
                    Utility.AddHUDText(Text.NotFollowing.Get());
                }
                else if (CanAddFollower())
                {
                    //start following
                    isFollower = true;
                    SetMinionObjectName();
                    Utility.AddHUDText(Text.Following.Get());
                }
                else
                {
                    Utility.AddHUDText(Text.NotEnoughWillpower.Get());
                }
                return true;
            }
            else if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Grab)
            {
                if (distance <= pushActivateDistance)
                {
                    //push minion out of the way
                    IEnumerator coroutine = Push();
                    StartCoroutine(coroutine);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Initializes properties specific to minions.
        /// </summary>
        private void Initialize(bool following)
        {
            isFollower = following;

            SetMinionObjectName();

            EnemySenses senses = GetComponent<EnemySenses>();
            senses.HearingRadius = 4.0f;

            //create invisible target for movement control
            proxyTarget = Utility.CreateTarget(Vector3.zero);

            lastSeenPlayerPosition = GameManager.Instance.PlayerController.transform.position;
        }


        /// <summary>
        /// Gives the minion GameObject an appropriate name property.
        /// The name is used to identify a creature as a minion, and determine its following/staying status.
        /// </summary>
        private void SetMinionObjectName()
        {
            DaggerfallEntityBehaviour creature = GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = creature.Entity as EnemyEntity;
            MobileEnemy mobileEnemy = entity.MobileEnemy;

            string entityName = TextManager.Instance.GetLocalizedEnemyName(mobileEnemy.ID);

            string statusName = isFollower ? PenwickFollowerPrefix : PenwickMinionPrefix;
            if (creature.Entity.Team != MobileTeams.PlayerAlly)
            {
                statusName = PenwickFormerMinionPrefix;
            }

            string nameFormat = statusName + "[{0}]";

            creature.gameObject.name = string.Format(nameFormat, entityName);
        }


        /// <summary>
        /// Handles minion movement when following the PC or being pushed.
        /// Manipulates an invisible target that is placed near the desired destination.
        /// The minion will be made hostile to the target.  The actual movement is performed
        /// by the standard DFU movement code.
        /// </summary>
        private void Guide()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            DaggerfallEntityBehaviour behaviour = GetComponent<DaggerfallEntityBehaviour>();
            EnemySenses senses = GetComponent<EnemySenses>();
            EnemyMotor motor = GetComponent<EnemyMotor>();

            if (HasPathTo(behaviour, player.transform.position))
            {
                lastSeenPlayerPosition = player.transform.position;
            }

            if (isBeingPushed)
            {
                //move toward the invisible push target
                senses.Target = null;
                motor.MakeEnemyHostileToAttacker(proxyTarget);
            }
            else if (isFollower)
            {
                DoFollow();
            }
        }


        /// <summary>
        /// Called by Guide(), controls PC follow logic.
        /// </summary>
        private void DoFollow()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            EnemySenses senses = GetComponent<EnemySenses>();
            EnemyMotor motor = GetComponent<EnemyMotor>();

            senses.SecondaryTarget = null;

            float proxyDistance = Vector3.Distance(proxyTarget.transform.position, transform.position);
            float playerDistance = Vector3.Distance(player.transform.position, transform.position);
            Vector3 playerDirection = (player.transform.position - transform.position).normalized;

            if (senses.Target == null && playerDistance > followTriggerDistance)
            {
                StartFollowPlayer();
            }
            else if (senses.Target == proxyTarget)
            {
                if (playerDistance < 3)
                {
                    //player is nearby, we can stop following proxyTarget
                    senses.Target = null;
                }
                else if (proxyDistance > 2.5f)
                {
                    //this might be necessary to reset target acquisition data
                    senses.Target = null;
                    motor.MakeEnemyHostileToAttacker(proxyTarget);
                }
                else
                {
                    senses.Target = null;
                }
            }
            else if (senses.Target != null)
            {
                //is apparently engaged with an enemy
                if (!senses.TargetInSight)
                {
                    //ignore unseen enemy targets
                    senses.Target = null;
                }
                else if (playerDistance > 13)
                {
                    //flee current combat and follow player instead
                    StartFollowPlayer();
                }
            }

            //If player too far away, wait for player to look away from minion, then teleport behind them
            float signedAngle = Vector3.SignedAngle(playerDirection, player.transform.forward, Vector3.up);
            if (playerDistance > 15 && Mathf.Abs(signedAngle) < 60)
            {
                if (autoTeleportMinions)
                {
                    //falling too far behind, just teleport
                    if (TeleportBehindPlayer())
                    {
                        senses.Target = null;
                    }
                }
            }

            if (senses.Target == proxyTarget)
            {
                //The proxy is completely invisible.
                //This is a hack to enable minion to be aware of it in EnemySenses logic.
                senses.HearingRadius = 100;
                senses.DetectedTarget = true;
            }
            else
            {
                //Otherwise, keep hearing radius lower so minion isn't constantly
                //chasing enemies through walls
                senses.HearingRadius = 4;
            }

        }


        /// <summary>
        /// Called by DoFollow() to have a minion start following the PC.
        /// </summary>
        private void StartFollowPlayer()
        {
            Vector3 location = GetFollowDestination();

            EnemySenses senses = GetComponent<EnemySenses>();
            EnemyMotor motor = GetComponent<EnemyMotor>();

            senses.Target = null;

            if (Vector3.Distance(location, transform.position) < 2)
            {
                return;
            }

            proxyTarget.transform.position = location;

            Vector3 direction = (proxyTarget.transform.position - transform.position).normalized;
            proxyTarget.transform.position += direction * 2;

            motor.MakeEnemyHostileToAttacker(proxyTarget);
        }


        /// <summary>
        /// Determines an appropriate place to put an invisible follow target.
        /// This will either be the PC, or the last seen location of the PC.
        /// </summary>
        private Vector3 GetFollowDestination()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            if (HasPathTo(GetComponent<DaggerfallEntityBehaviour>(), player.transform.position))
            {
                //player is visible, just move towards them
                Vector3 aStepBack = (player.transform.position - transform.position).normalized;
                return player.transform.position - aStepBack;
            }

            return lastSeenPlayerPosition;
        }


        /// <summary>
        /// Teleports following minions behind the player.
        /// </summary>
        private bool TeleportBehindPlayer()
        {
            CharacterController player = GameManager.Instance.PlayerController;

            //check for empty space behind player
            RaycastHit hit;
            Ray ray = new Ray(player.transform.position, -player.transform.forward);
            if (Physics.Raycast(ray, out hit, 3f))
            {
                //need open space behind
                return false;
            }

            Vector3 position = player.transform.position - (player.transform.forward * 2);
            position += Vector3.up;

            Collider[] colliders = Physics.OverlapSphere(position, 0.65f);
            if (colliders.Length > 0)
            {
                return false;
            }


            ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out hit, 3))
            {
                transform.position = position;
                transform.rotation = player.transform.rotation;
                GameObjectHelper.AlignControllerToGround(GetComponent<CharacterController>(), 4);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Called when the player activates a minion while in 'Grab' mode.
        /// The invisible proxy target used for guiding the minion is placed nearby.
        /// </summary>
        private IEnumerator Push()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            Vector3 pushDirection = (transform.position - player.transform.position).normalized;

            Ray ray = new Ray(transform.position, pushDirection);

            RaycastHit hit;

            Physics.Raycast(ray, out hit, 4);

            if (hit.collider && (hit.collider is TerrainCollider || hit.collider is MeshCollider))
            {
                //hitting a wall, set target just in front of
                proxyTarget.transform.position = hit.point - (pushDirection * 0.1f);
            }
            else
            {
                proxyTarget.transform.position = transform.position + (pushDirection * 4);
            }

            isBeingPushed = true;

            yield return new WaitForSeconds(1.0f);

            isBeingPushed = false;
        }


    } //class PenwickMinion

} //namespace
