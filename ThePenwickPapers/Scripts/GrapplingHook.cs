// Project:     GrapplingHook, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using System.Collections;
using UnityEngine;


namespace ThePenwickPapers
{

    public class GrapplingHook : MonoBehaviour
    {
        private const int hookAndRopeItemIndex = 544; //value from thiefoverhaul/skulduggery mod

        public const string PenwickHookName = "Penwick Hook";
        public const string PenwickRopeName = "Penwick Rope";
        public const float MaxRopeLength = 12.0f;

        private static GameObject hook;
        private static GameObject rope;



        /// <summary>
        /// Checks activated location to see if it conditions are appropriate to attach a grappling hook.
        /// If deemed appropriate, then a coroutine is called to create the hook and rope.
        /// </summary>
        public static bool AttemptHook(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;

            bool hitTerrain = hitInfo.collider is TerrainCollider || hitInfo.collider is MeshCollider;
            if (!hitTerrain)
                return false;

            //Don't hook action objects, like switches and platforms
            if (hitInfo.transform.gameObject.GetComponent<DaggerfallAction>())
                return false;

            //check for equipped hook and rope item, from Skulduggery mod
            bool hookAndRopeEquipped = false;
            foreach (DaggerfallUnityItem item in GameManager.Instance.PlayerEntity.ItemEquipTable.EquipTable)
            {
                if (item != null && item.TemplateIndex == hookAndRopeItemIndex) 
                    hookAndRopeEquipped = true;
            }

            if (!hookAndRopeEquipped)
                return false;

            Vector3 anchorPoint;

            anchorPoint = FindUpperLedgeAnchor(hitInfo);
            if (anchorPoint == Vector3.zero)
                anchorPoint = FindLowerLedgeAnchor(hitInfo);

            if (anchorPoint == Vector3.zero)
                return false;

            float ropeLength;
            RaycastHit floorHit;
            if (Physics.Raycast(anchorPoint, Vector3.down, out floorHit, MaxRopeLength))
                ropeLength = floorHit.distance - 0.4f;
            else
                ropeLength = MaxRopeLength;

            if (ropeLength < 3f)
                return false; //shouldn't need a rope for such a short distance

            IEnumerator coroutine = SetHookCoroutine(anchorPoint, ropeLength);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);

            return true;
        }


        /// <summary>
        /// Tries to locate appropriate anchoring point for hook.
        /// </summary>
        private static Vector3 FindUpperLedgeAnchor(RaycastHit hitInfo)
        {
            //back away from wall a tad
            Vector3 anchorPoint = hitInfo.point + hitInfo.normal * 0.05f;

            Ray ray;

            //check if position above contact point is blocked
            ray = new Ray(anchorPoint, Vector3.up);
            if (Physics.Raycast(ray, 0.5f))
                return Vector3.zero;

            //Won't assume all normals are level with coordinate axes
            Vector3 effectiveNormal = Vector3.ProjectOnPlane(hitInfo.normal, Vector3.up);

            //up and over ledge
            anchorPoint += Vector3.up * 0.5f;
            ray = new Ray(anchorPoint, -effectiveNormal);
            if (Physics.Raycast(ray, 0.1f))
                return Vector3.zero;

            anchorPoint -= effectiveNormal * 0.1f;

            //Find height of ledge by looking down
            RaycastHit ledgeHit;
            ray = new Ray(anchorPoint, Vector3.down);
            if (!Physics.Raycast(ray, out ledgeHit, 0.5f))
                return Vector3.zero;

            //anchor the hook close enough to the wall to grab wall if needed
            anchorPoint = hitInfo.point + effectiveNormal * 0.05f;
            anchorPoint.y = ledgeHit.point.y;

            return anchorPoint;
        }


        /// <summary>
        /// Tries to locate appropriate anchoring point for hook.
        /// </summary>
        private static Vector3 FindLowerLedgeAnchor(RaycastHit hitInfo)
        {
            Vector3 throwDirection = (hitInfo.point - GameManager.Instance.PlayerController.transform.position).normalized;
            Vector3 lookDirection = Vector3.ProjectOnPlane(throwDirection, Vector3.up).normalized;

            Vector3 anchorPoint = hitInfo.point;
            anchorPoint -= throwDirection * 0.02f; //back up a bit
            anchorPoint.y += 0.05f;

            Ray ray;

            ray = new Ray(anchorPoint, lookDirection);
            if (Physics.Raycast(ray, 0.6f))
                return Vector3.zero; //expecting open space

            //extend into open space past expected ledge
            anchorPoint += lookDirection * 0.6f;

            //drop below ledge
            anchorPoint.y -= 0.1f;

            if (Physics.CheckSphere(anchorPoint, 0.1f))
                return Vector3.zero; //was expecting open space

            ray = new Ray(anchorPoint, -lookDirection);
            RaycastHit ledgeHit;
            if (!Physics.Raycast(ray, out ledgeHit, 0.6f))
                return Vector3.zero; //should have hit ledge wall there

            //anchor the hook close enough to the wall to grab wall if needed
            anchorPoint = ledgeHit.point + lookDirection * 0.05f;
            anchorPoint.y = hitInfo.point.y;

            return anchorPoint;
        }


        /// <summary>
        /// Coroutine to create the hook and rope GameObjects at the specified anchoring point
        /// with the appropriate sound effects.
        /// </summary>
        private static IEnumerator SetHookCoroutine(Vector3 location, float length)
        {
            if (hook)
            {
                //have experienced mysterious problems with previous hooks not disappearing
                hook.SetActive(false);
                GameObject.Destroy(hook);
            }

            float distance = Vector3.Distance(location, GameManager.Instance.PlayerController.transform.position);
            if (distance > 4)
                DaggerfallUI.Instance.DaggerfallAudioSource.PlayOneShot(SoundClips.SwingMediumPitch2, 0f, distance / 20);

            //provide some time for hook to travel to destination
            yield return new WaitForSeconds(distance / 7f);

            //play a clanking noise
            DaggerfallUI.Instance.DaggerfallAudioSource.PlayClipAtPoint(SoundClips.Parry1, location);


            //hook and rope turns invisible when city lights come on, see worldtime.oncitylightson and DayNight class

            //create the basic hook
            hook = new GameObject(PenwickHookName);

            Texture2D hookTexture = ThePenwickPapersMod.Instance.GrapplingHookTexture;

            DaggerfallBillboard dfBillboard = hook.AddComponent<DaggerfallBillboard>();
            Vector2 hookSize = new Vector2((float)hookTexture.width / 70f, (float)hookTexture.height / 70f);
            dfBillboard.SetMaterial(hookTexture, hookSize, false);

            hook.transform.parent = GameObjectHelper.GetBestParent();
            hook.transform.position = location;

            MeshRenderer meshRenderer = hook.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            //for making rope creak noises
            hook.AddComponent<DaggerfallAudioSource>();


            //create variable length rope attached to the hook
            rope = new GameObject(PenwickRopeName);
            rope.transform.parent = hook.transform;

            Texture2D ropeTexture = ThePenwickPapersMod.Instance.RopeTexture;

            dfBillboard = rope.AddComponent<DaggerfallBillboard>();
            Vector2 ropeSize = new Vector2(0.05f, length);
            dfBillboard.SetMaterial(ropeTexture, ropeSize, false);

            //rope placement depends on hook placement
            float y = -length / 2;
            y -= hookSize.y / 2;

            rope.transform.localPosition = new Vector3(0f, y, 0f);

            meshRenderer = rope.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            //tiling texture
            meshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, length * 10);

            //add the collider for climbing, kind of the point
            BoxCollider collider = rope.AddComponent<BoxCollider>();
            Vector3 size = collider.size;
            size.x *= 2; //make the collider a bit wider so it is easier to climb onto
            collider.size = size;

            //handles creaking noises while climbing rope
            rope.AddComponent<RopeClimbing>(); 

        }



    }



    class RopeClimbing : MonoBehaviour
    {
        private float lastCreakTime;

        private void Update()
        {
            if (GameManager.IsGamePaused)
                return;

            ClimbingMotor climbingMotor = GameManager.Instance.ClimbingMotor;
            CharacterController controller = GameManager.Instance.PlayerController;
            DaggerfallAudioSource dfAudio = transform.parent.GetComponent<DaggerfallAudioSource>();
            DaggerfallBillboard dfBillboard = GetComponent<DaggerfallBillboard>();


            if (!climbingMotor.IsClimbing)
            {
                dfAudio.AudioSource.Stop();
                return;
            }
            

            Vector3 ropeXZ = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 playerXZ = Vector3.ProjectOnPlane(climbingMotor.transform.position, Vector3.up);

            float distance = Vector3.Distance(playerXZ, ropeXZ);

            if (distance >= controller.radius + 0.06f)
                return;

            float length = dfBillboard.Summary.Size.y;
            float ropeY = transform.position.y;
            float playerY = climbingMotor.transform.position.y;
            if (Mathf.Abs(playerY - ropeY) > (length + 1f) / 2f)
                return;

            //occasionally make creaking sounds
            if (Time.time > lastCreakTime + 2.0f && Dice100.SuccessRoll(2))
            {
                if (Dice100.SuccessRoll(50))
                    dfAudio.AudioSource.PlayOneShot(ThePenwickPapersMod.Instance.Creak1, 0.5f);
                else
                    dfAudio.AudioSource.PlayOneShot(ThePenwickPapersMod.Instance.Creak2, 0.5f);

                lastCreakTime = Time.time;
            }

        }



    }


}