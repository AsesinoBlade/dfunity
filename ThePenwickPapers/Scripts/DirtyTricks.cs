// Project:     Dirty Tricks, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;

namespace ThePenwickPapers
{

    public class DirtyTricks
    {
        private const float blindRange = 4.0f;
        private const float bootRange = 7.0f; //max range at which player can *begin* a boot attempt
        private const float peepRange = 0.6f;

        public static bool EnableBlinding = false;
        public static bool EnableChock = false;
        public static bool EnableDiversion = false;
        public static bool EnableTheBoot = false;
        public static bool EnablePeep = false;

        private static float lastDiversionAttempt = -15f;
        private static float lastPlayerBlindAttempt = -15f;
        private static float lastEnemyBlindAttempt = -15f;
        private static bool peeping;

        private static readonly HashSet<MobileTypes> sneakyTypes = new HashSet<MobileTypes>() {
            MobileTypes.Bard, MobileTypes.Burglar, MobileTypes.Rogue, MobileTypes.Thief
        };


        /// <summary>
        /// Check if the activation event triggers any dirty tricks.  If so, the dirty deed
        /// will be done.
        /// </summary>
        public static bool CheckAttemptTrick(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;

            PlayerActivateModes mode = GameManager.Instance.PlayerActivate.CurrentMode;

            DaggerfallEntityBehaviour creature = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

            if (creature)
            {
                //If activating a creature, make sure it is a non-allied enemy entity.
                //We don't want to do dirty tricks to allies or civilians.
                if (creature.Entity.Team == MobileTeams.PlayerAlly)
                    return false;
                else if (!(creature.Entity is EnemyEntity))
                    return false;
            }

            if (mode == PlayerActivateModes.Steal)
            {
                bool isTerrain = hitInfo.collider is TerrainCollider || hitInfo.collider is MeshCollider;
                DaggerfallActionDoor door = hitInfo.collider.GetComponent<DaggerfallActionDoor>();

                if (EnableBlinding && creature && hitInfo.distance <= blindRange)
                {
                    bool isFacing = IsFacingTrickster(creature, GameManager.Instance.PlayerEntityBehaviour);
                    if (isFacing)
                    {
                        AttemptBlindByPlayer(creature);
                        return true;
                    }
                }
                else if (EnableChock && door && door.IsClosed && hitInfo.distance <= PlayerActivate.DoorActivationDistance)
                {
                    if (!door.IsLocked)
                        return ChockDoor(door);
                    else if (door.FailedSkillLevel < 0) //indicates a chocked door
                        return UnchockDoor(door);
                    else    
                        return false;
                }
                else if (EnableDiversion && isTerrain && hitInfo.distance > 5)
                {
                    AttemptDiversion(hitInfo);
                    return true;
                }
            }
            else if (EnableTheBoot && mode == PlayerActivateModes.Grab)
            {
                if (creature && hitInfo.distance <= bootRange)
                {
                    AttemptBoot(creature);
                    return true;
                }
            }
            else if (EnablePeep && mode == PlayerActivateModes.Info && hitInfo.distance <= peepRange)
            {
                DaggerfallActionDoor door = hitInfo.collider.GetComponent<DaggerfallActionDoor>();
                if (door && door.IsClosed)
                {
                    if (!peeping)
                        ThePenwickPapersMod.Instance.StartCoroutine(PeepDoor(hitInfo, door));

                    return true;
                }

            }

            return false;
        }


        /// <summary>
        /// Certain enemy classes in range will periodically attempt to blind the player.
        /// </summary>
        public static void CheckEnemyBlindAttempt()
        {
            //only check for blind attempts once in a while
            if (Dice100.FailedRoll(1))
                return;

            if (Time.time < lastEnemyBlindAttempt + 25)
                return;

            List<DaggerfallEntityBehaviour> enemyBehaviours = Utility.GetNearbyEntities();

            foreach (DaggerfallEntityBehaviour behaviour in enemyBehaviours)
            {
                EnemySenses senses = behaviour.GetComponent<EnemySenses>();

                if (senses == null || senses.Target == null)
                    continue;
                else if (senses.SightRadius < blindRange)
                    continue;

                EnemyEntity entity = behaviour.Entity as EnemyEntity;
                if (entity.IsParalyzed)
                    continue;
                else if (behaviour.gameObject.name.StartsWith(IllusoryDecoy.DecoyGameObjectPrefix))
                    continue;
                else if (!sneakyTypes.Contains((MobileTypes)entity.MobileEnemy.ID))
                    continue;    //only certain enemy types will attempt blinding
                else if (Vector3.Distance(senses.Target.transform.position, behaviour.transform.position) > blindRange)
                    continue;
                else if (!IsFacingTrickster(senses.Target, behaviour))
                    continue;

                AttemptBlind(behaviour, senses.Target);

                lastEnemyBlindAttempt = Time.time;

                if (senses.Target == GameManager.Instance.PlayerEntityBehaviour)
                    GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Streetwise, 1);

                break;
            }


        }


        /// <summary>
        /// Checks if the target (victim) is facing towards the trickster.
        /// </summary>
        private static bool IsFacingTrickster(DaggerfallEntityBehaviour victim, DaggerfallEntityBehaviour trickster)
        {
            float angle = Vector3.SignedAngle(victim.transform.forward, trickster.transform.forward, Vector3.up);

            angle = 180 - Mathf.Abs(angle);

            return angle < 25;
        }


        /// <summary>
        /// Player attempts to blind target creature
        /// </summary>
        private static void AttemptBlindByPlayer(DaggerfallEntityBehaviour creature)
        {
            DaggerfallEntityBehaviour playerBehaviour = GameManager.Instance.PlayerEntityBehaviour;
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                return;
            else if (playerEntity.IsParalyzed)
                return;
            else if (Utility.IsBlind(playerBehaviour))
                return;

            float rechargeTime = (140 - playerEntity.Stats.LiveLuck) / 8.0f;
            rechargeTime = Mathf.Clamp(rechargeTime, 5, 15);

            if (Time.time < lastPlayerBlindAttempt + rechargeTime)
                return;

            lastPlayerBlindAttempt = Time.time;

            //always at least some skill increment
            playerEntity.TallySkill(DFCareer.Skills.Pickpocket, 1);

            if (AttemptBlind(playerBehaviour, creature))
            {
                //another tally on success
                playerEntity.TallySkill(DFCareer.Skills.Pickpocket, 1);
            }

        }


        private static readonly HashSet<MobileTypes> resistant = new HashSet<MobileTypes>() {
            MobileTypes.FireAtronach, MobileTypes.FleshAtronach, MobileTypes.IceAtronach, MobileTypes.IronAtronach,
            MobileTypes.Spider, MobileTypes.GiantScorpion, MobileTypes.Spriggan
        };
        /// <summary>
        /// Performs skill checks to see if a blinding attempt succeeds.
        /// Applies Blind incumbent effect on success.
        /// </summary>
        private static bool AttemptBlind(DaggerfallEntityBehaviour trickster, DaggerfallEntityBehaviour victim)
        {
            //attempting blinding maneuver, play sound
            DaggerfallAudioSource dfAudioSource = trickster.GetComponent<DaggerfallAudioSource>();
            dfAudioSource.PlayOneShot(SoundClips.SwingHighPitch, 1, 0.5f);

            //Check chance of success by comparing trickster pickpocket&agility versus victim streetwise&agility
            int offense = trickster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Pickpocket);
            offense += trickster.Entity.Stats.LiveAgility / 2;

            int streetwise = victim.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);

            if (victim.EntityType != EntityTypes.Player)
            {
                EnemyEntity enemy = victim.Entity as EnemyEntity;

                if (Blind.Immune.Contains((MobileTypes)enemy.MobileEnemy.ID))
                    return false;
                else if (resistant.Contains((MobileTypes)enemy.MobileEnemy.ID) && Dice100.SuccessRoll(50))
                    return false;

                //adjusting enemy streetwise skill
                bool isSneaky = sneakyTypes.Contains((MobileTypes)enemy.MobileEnemy.ID);
                streetwise -= enemy.Level * (isSneaky ? 2 : 4);
            }

            int defense = streetwise;
            defense += victim.Entity.Stats.LiveAgility / 2;

            int chance = Mathf.Clamp(50 + offense - defense, 5, 95);

            if (Dice100.SuccessRoll(chance))
            {
                BlindVictim(victim);
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Performs animation and knockback, then applies Blind incumbent effect on victim.
        /// </summary>
        private static void BlindVictim(DaggerfallEntityBehaviour victim)
        {
            //Do knockback and dirt animation on non-player victims
            if (victim.EntityType != EntityTypes.Player)
            {
                EnemyMotor motor = victim.GetComponent<EnemyMotor>();
                EnemyEntity enemy = victim.Entity as EnemyEntity;

                bool knockable = victim.EntityType == EntityTypes.EnemyClass || enemy.MobileEnemy.Weight > 0;
                if (knockable && motor.KnockbackSpeed < 5)
                {
                    motor.KnockbackSpeed = 15 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10);
                    motor.KnockbackDirection = -victim.transform.forward;
                }

                //show graphic dirt-throw
                Vector3 pos = victim.transform.position;
                MobileUnit unit = victim.GetComponentInChildren<MobileUnit>();
                float eyeHeight = pos.y + unit.GetSize().y / 2.0f - 0.3f;
                Vector3 impactPosition = new Vector3(pos.x, eyeHeight, pos.z);
                impactPosition += victim.transform.forward * 0.2f;

                EnemyBlood blood = victim.GetComponent<EnemyBlood>();
                if (blood)
                {
                    blood.ShowBloodSplash(2, impactPosition);
                }
            }

            //Finally, induce an incumbent blinding effect
            EntityEffectManager manager = victim.GetComponent<EntityEffectManager>();

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();
            settings.DurationBase = 2; //should be 5-10 seconds
            settings.DurationPlus = 0;
            settings.DurationPerLevel = 1;

            EntityEffectBundle bundle = manager.CreateSpellBundle(Blind.EffectKey, ElementTypes.None, settings);
            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;
            manager.AssignBundle(bundle, flags);
        }


        /// <summary>
        /// Effectively 'locks' a closed, unlocked door with a makeshift chock.
        /// Must be an inward opening door.
        /// </summary>
        private static bool ChockDoor(DaggerfallActionDoor door)
        {
            Vector3 playerForward = GameManager.Instance.PlayerObject.transform.forward;
            if (Vector3.Angle(playerForward, door.transform.forward) <= 90)
                return false; //doors opening outward can't be chocked

            int lockpicking = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
            if (lockpicking < 20)
            {
                string skillName = DaggerfallUnity.Instance.TextProvider.GetSkillName(DFCareer.Skills.Lockpicking);
                Utility.AddHUDText(Text.NotEnoughLockpickingSkill.Get(skillName));
                return true;
            }

            int lockLevel = lockpicking / 5 + 1;
            door.CurrentLockValue = lockLevel;
            door.FailedSkillLevel = -1; //flag to signify a chocked door

            DaggerfallAudioSource dfAudioSource = door.GetComponent<DaggerfallAudioSource>();
            if (dfAudioSource != null)
                dfAudioSource.PlayOneShot(door.PickedLockSound);

            Utility.AddHUDText(Text.DoorChocked.Get());

            return true;
        }


        /// <summary>
        /// Unchocks a chocked door.
        /// Must be an inward opening door.
        /// </summary>
        private static bool UnchockDoor(DaggerfallActionDoor door)
        {
            Vector3 playerForward = GameManager.Instance.PlayerObject.transform.forward;
            if (Vector3.Angle(playerForward, door.transform.forward) <= 90)
                return false; //doors opening outward can't be unchocked

            door.StartingLockValue = 0;
            door.FailedSkillLevel = 0;

            DaggerfallAudioSource dfAudioSource = door.GetComponent<DaggerfallAudioSource>();
            if (dfAudioSource != null)
                dfAudioSource.PlayOneShot(door.PickedLockSound);

            Utility.AddHUDText(Text.DoorUnchocked.Get());

            return true;
        }


        /// <summary>
        /// Tosses a pebble to distract unaware/unengaged opponents.
        /// </summary>
        private static void AttemptDiversion(RaycastHit hitInfo)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                return;

            float rechargeTime = (160 - GameManager.Instance.PlayerEntity.Stats.LiveLuck) / 10.0f;
            rechargeTime = Mathf.Clamp(rechargeTime, 4, 20);

            if (Time.time < lastDiversionAttempt + rechargeTime)
                return;

            lastDiversionAttempt = Time.time;

            Vector3 location = hitInfo.point + (hitInfo.normal * 0.1f);

            DaggerfallAudioSource audio = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();
            audio.PlayOneShot(SoundClips.SwingHighPitch, 1, 0.3f);

            DaggerfallEntityBehaviour diversion = Utility.CreateTarget(location);

            IEnumerator coroutine = DoDiversion(hitInfo.distance, diversion);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);

        }


        /// <summary>
        /// A coroutine that handles the diversion mechanism on opponents in range.
        /// </summary>
        private static IEnumerator DoDiversion(float distance, DaggerfallEntityBehaviour diversion)
        {
            yield return new WaitForSeconds(distance * 0.1f);

            Vector3 location = diversion.transform.position;

            DaggerfallAudioSource dfAudioSource = DaggerfallUI.Instance.GetComponent<DaggerfallAudioSource>();
            dfAudioSource.PlayClipAtPoint(SoundClips.DiceRoll2, location, 1.0f);

            List<DaggerfallEntityBehaviour> enemyBehaviours = Utility.GetNearbyEntities(location, 11);

            foreach (DaggerfallEntityBehaviour behaviour in enemyBehaviours)
            {
                if (!(behaviour.Entity is EnemyEntity))
                    continue;

                EnemyEntity entity = behaviour.Entity as EnemyEntity;
                EnemySenses senses = behaviour.GetComponent<EnemySenses>();

                if (senses == null || senses.Target != null)
                    continue;    //if target is engaged, ignore diversions
                else if (behaviour.gameObject.name.StartsWith(IllusoryDecoy.DecoyGameObjectPrefix))
                    continue;
                else if (entity.Team == MobileTeams.PlayerAlly)
                    continue;

                EnemyMotor motor = behaviour.GetComponent<EnemyMotor>();
                motor.MakeEnemyHostileToAttacker(diversion);
            }

            yield return new WaitForSeconds(4);

            //finally, destroy the temporary diversion object
            GameObject.Destroy(diversion.gameObject);
        }


        /// <summary>
        /// Attempts to apply a boot to the face.
        /// </summary>
        private static void AttemptBoot(DaggerfallEntityBehaviour victim)
        {
            WeaponManager manager = GameManager.Instance.WeaponManager;

            if (manager.ScreenWeapon.IsAttacking() || ThePenwickPapersMod.TheBoot.IsAttacking())
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.SwingMediumPitch);

            ThePenwickPapersMod.TheBoot.ShowWeapon = true;
            ThePenwickPapersMod.TheBoot.OnAttackDirection(WeaponManager.MouseDirections.Up);

            IEnumerator coroutine = BootCoroutine(victim);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Coroutine that handles TheBoot animation and triggers a knockback attempt.
        /// </summary>
        private static IEnumerator BootCoroutine(DaggerfallEntityBehaviour victim)
        {
            int hitFrame = ThePenwickPapersMod.TheBoot.GetHitFrame();

            while (ThePenwickPapersMod.TheBoot.IsAttacking() && ThePenwickPapersMod.TheBoot.GetCurrentFrame() < hitFrame)
                yield return new WaitForSeconds(0.02f);

            CheckKnockAttempt(victim);

            while (ThePenwickPapersMod.TheBoot.IsAttacking() && ThePenwickPapersMod.TheBoot.GetCurrentFrame() < 4)
                yield return null;

            ThePenwickPapersMod.TheBoot.ChangeWeaponState(WeaponStates.Idle);
            ThePenwickPapersMod.TheBoot.ShowWeapon = false;
        }


        /// <summary>
        /// Handles contested attempt to knockback the victim; performs knockback on success.
        /// </summary>
        private static void CheckKnockAttempt(DaggerfallEntityBehaviour victim)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            CharacterController playerController = GameManager.Instance.PlayerController;

            float fatigueLossMultiplier = 1.0f;
            if (player.Career.Athleticism)
                fatigueLossMultiplier = (player.ImprovedAthleticism) ? 0.6f : 0.8f;

            player.DecreaseFatigue((int)(22 * fatigueLossMultiplier));

            if (!(victim.Entity is EnemyEntity))
                return;

            EnemyEntity enemy = victim.Entity as EnemyEntity;

            EnemyMotor motor = victim.GetComponent<EnemyMotor>();
            motor.MakeEnemyHostileToAttacker(GameManager.Instance.PlayerEntityBehaviour);

            //Check if victim is at the contact point
            RaycastHit hit;
            if (!Physics.Raycast(playerMotor.transform.position, playerMotor.transform.forward, out hit, 2.3f))
                return;

            if (!hit.collider || hit.collider.GetComponent<DaggerfallEntityBehaviour>() != victim)
                return;

            int offense = player.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike);
            offense += player.Stats.GetLiveStatValue(DFCareer.Stats.Agility) / 2;

            int defense = enemy.Skills.GetLiveSkillValue(DFCareer.Skills.Dodging);

            //Enemies have skill of Base + level * 5 skills, which is a bit high.  Adjusting...
            defense -= enemy.Level * 3;

            defense += enemy.Stats.GetLiveStatValue(DFCareer.Stats.Agility) / 2;

            if (victim.GetComponent<DaggerfallEnemy>().MobileUnit.IsAttacking())
                defense -= 30;

            if (!IsFacingTrickster(victim, GameManager.Instance.PlayerEntityBehaviour))
                defense = 0;
            else if (Utility.IsBlind(victim))
                defense = 0;
            else if (victim.Entity.IsParalyzed)
                defense = 0;

            int hitChance = Mathf.Clamp(50 + offense - defense, 10, 90);

            if (Dice100.FailedRoll(hitChance))
                return;

            DaggerfallAudioSource dfAudio = victim.GetComponent<DaggerfallAudioSource>();
            dfAudio.PlayOneShot(SoundClips.Hit2);

            Vector3 playerVelocity = playerController.velocity;
            Vector3 direction = (victim.transform.position - playerMotor.transform.position).normalized;
            float effective = Vector3.Project(playerVelocity, direction).magnitude;

            if (hitChance > 100)
                effective *= 1.3f;

            float playerStrength = player.Stats.GetLiveStatValue(DFCareer.Stats.Strength);
            effective += playerStrength / 10; 

            int creatureWeight = enemy.GetWeightInClassicUnits();
            if (enemy.EntityType == EntityTypes.EnemyClass)
                creatureWeight = 500;
            if (creatureWeight == 0)
                return; //ghosts etc.

            effective *= 800.0f / creatureWeight;

            effective = Mathf.Clamp(effective, 0, 50);

            if (playerMotor.IsSwimming)
                effective /= 4;

            motor.KnockbackSpeed += effective;
            motor.KnockbackDirection = direction;

            //critical strike has a large advancement multiplier (8)
            GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.CriticalStrike, 4);
        }


        /// <summary>
        /// Peeps through door hole, or under door if crouching.
        /// Stops peeping when a key is pressed.
        /// </summary>
        private static IEnumerator PeepDoor(RaycastHit hitInfo, DaggerfallActionDoor actionDoor)
        {
            Collider door = actionDoor.GetComponent<BoxCollider>();

            bool isCrouching = GameManager.Instance.PlayerMotor.IsCrouching;

            Vector3 pos;
            if (isCrouching)
            {
                //Looking under door
                RaycastHit floorHit;
                if (!Physics.Raycast(door.bounds.center, Vector3.down, out floorHit))
                    yield break;

                pos = floorHit.point;
                pos.y += 0.1f;
            }
            else
            {
                pos = door.bounds.center;

                //Peephole can be located at various positions on door.
                //Using a consistently repeatable algorithm based on door position.
                float hOffset = (Mathf.Abs(door.bounds.center.x * 13.7f % 10) - 5) / 15f; //-0.33 to 0.33
                float vOffset = Mathf.Abs(door.bounds.center.z * 13.7f % 10) / 12f; //0.0 to 0.833
                pos.y = GameManager.Instance.PlayerMotor.transform.position.y;
                pos.y += vOffset;
                bool northSouthDoor = Vector3.Angle(hitInfo.normal, Vector3.forward) != 90;
                if (northSouthDoor)
                    pos.x += hOffset;
                else
                    pos.z += hOffset;
            }

            peeping = true;

            GameObject peeper = new GameObject("Penwick Peeper");

            peeper.transform.position = pos;

            Camera camera = peeper.AddComponent<Camera>();
            camera.transform.LookAt(pos - hitInfo.normal);
            camera.depth = GameManager.Instance.MainCamera.depth + 1;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            if (isCrouching)
                camera.transform.Rotate(Vector3.forward, -90);

            Texture2D peepTexture;
            if (isCrouching)
                peepTexture = ThePenwickPapersMod.Instance.PeepSlitTexture;
            else
                peepTexture = ThePenwickPapersMod.Instance.PeepHoleTexture;

            DaggerfallHUD hud = DaggerfallUI.Instance.DaggerfallHUD;
            hud.ParentPanel.BackgroundTexture = peepTexture;
            hud.ParentPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

            yield return new WaitForSeconds(0.2f); //should be enough time to finish initial mouse click
            
            while (actionDoor.IsClosed && !InputManager.Instance.AnyKeyDown)
            {
                InputManager.Instance.ClearAllActions();
                yield return null;
            }

            GameObject.Destroy(peeper);
            hud.ParentPanel.BackgroundTexture = null;
            peeping = false;
        }




    } //class DirtyTricks


} //namespace