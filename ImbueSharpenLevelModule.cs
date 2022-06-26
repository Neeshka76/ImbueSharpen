using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using SnippetCode;

namespace ImbueSharpen
{
    public class ImbueSharpenLevelModule : LevelModule
    {
        private Item rightHandItem;
        private Item leftHandItem;
        private bool leftCollidingWithOtherHand = false;
        private Vector3 leftPositionOnSwordStart = Vector3.zero;
        private bool leftSharpenDone = false;
        private bool rightCollidingWithOtherHand = false;
        private Vector3 rightPositionOnSwordStart = Vector3.zero;
        private bool rightSharpenDone = false;
        public override IEnumerator OnLoadCoroutine()
        {
            EventManager.onPossess += EventManager_onPossess;
            EventManager.onUnpossess += EventManager_onUnpossess;
            return base.OnLoadCoroutine();
        }

        private void EventManager_onUnpossess(Creature creature, EventTime eventTime)
        {
            if (EventTime.OnStart == eventTime)
            {
                creature.handLeft.OnGrabEvent -= HandLeft_OnGrabEvent;
                creature.handLeft.OnUnGrabEvent -= HandLeft_OnUnGrabEvent;
                creature.handRight.OnGrabEvent -= HandRight_OnGrabEvent;
                creature.handRight.OnUnGrabEvent -= HandRight_OnUnGrabEvent;
            }
        }

        private void EventManager_onPossess(Creature creature, EventTime eventTime)
        {
            if (EventTime.OnEnd == eventTime)
            {
                creature.handLeft.OnGrabEvent += HandLeft_OnGrabEvent;
                creature.handLeft.OnUnGrabEvent += HandLeft_OnUnGrabEvent;
                creature.handRight.OnGrabEvent += HandRight_OnGrabEvent;
                creature.handRight.OnUnGrabEvent += HandRight_OnUnGrabEvent;
            }
        }

        private void HandRight_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                rightHandItem = null;
            }
        }

        private void HandRight_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                rightHandItem = handle.item;
            }
        }

        private void HandLeft_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                leftHandItem = null;
            }
        }

        private void HandLeft_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                leftHandItem = handle.item;
            }
        }

        public override void Update()
        {
            base.Update();

            if (leftHandItem != null)
            {
                if (leftHandItem.mainHandler.IsPlayer())
                {
                    if (leftHandItem.mainHandler.playerHand.controlHand.alternateUsePressed)
                    {
                        //LeftSharpeningMethod(leftHandItem, 0.3f, 0.5f);
                        LeftSharpeningMethod(leftHandItem, 0.1f, 0.3f);
                    }
                }
            }
            if (rightHandItem != null)
            {
                if (rightHandItem.mainHandler.IsPlayer())
                {
                    if (rightHandItem.mainHandler.playerHand.controlHand.alternateUsePressed)
                    {
                        RightSharpeningMethod(rightHandItem, 0.1f, 0.3f);
                    }
                }
            }
        }

        private void LeftSharpeningMethod(Item item, float distance, float speed)
        {
            if (item.GetComponent<CollisionHandler>().isColliding)
            {
                foreach (CollisionInstance collisionInstance in item.GetComponent<CollisionHandler>().collisions)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart is RagdollPart part && (part.ragdoll.creature.handLeft == item.mainHandler.otherHand || part.ragdoll.creature.handRight == item.mainHandler.otherHand) && !leftCollidingWithOtherHand)
                        {
                            leftCollidingWithOtherHand = true;
                            leftPositionOnSwordStart = item.mainHandler.otherHand.transform.position;
                        }
                    }
                    // Colliding with another item
                    else
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.item?.mainHandler == item.mainHandler.otherHand && !leftCollidingWithOtherHand)
                        {
                            leftCollidingWithOtherHand = true;
                            leftPositionOnSwordStart = item.mainHandler.otherHand.grabbedHandle.item.transform.position;
                        }
                    }
                }
                if (leftCollidingWithOtherHand && !leftSharpenDone)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (Vector3.Distance(item.mainHandler.otherHand.transform.position, leftPositionOnSwordStart) > distance && Vector3.Dot(item.mainHandler.otherHand.Velocity(), item.rb.velocity) < -speed)
                        {
                            //Activate Imbue
                            item.UnImbueItem();
                            if (item.mainHandler.caster.spellInstance != null)
                                item.ImbueItem(item.mainHandler.caster.spellInstance.id);
                            leftSharpenDone = true;
                        }
                    }
                    // Colliding with another item
                    else
                    {
                        if (Vector3.Distance(item.mainHandler.otherHand.grabbedHandle.item.transform.position, leftPositionOnSwordStart) > distance && Vector3.Dot(item.mainHandler.otherHand.grabbedHandle.item.rb.velocity, item.rb.velocity) < -speed)
                        {
                            //Activate Imbue
                            item.UnImbueItem();
                            if (item.mainHandler.caster.spellInstance != null)
                                item.ImbueItem(item.mainHandler.caster.spellInstance.id);
                            if (item.mainHandler.otherHand.caster.spellInstance != null && item.mainHandler.otherHand.playerHand.controlHand.alternateUsePressed)
                            {
                                item.mainHandler.otherHand.grabbedHandle.item.UnImbueItem();
                                if (item.mainHandler.otherHand.caster.spellInstance != null)
                                    item.mainHandler.otherHand.grabbedHandle.item.ImbueItem(item.mainHandler.otherHand.caster.spellInstance.id);
                            }
                            leftSharpenDone = true;
                        }
                    }
                }
            }
            else
            {
                if (leftCollidingWithOtherHand)
                {
                    leftCollidingWithOtherHand = false;
                    leftPositionOnSwordStart = Vector3.zero;
                    leftSharpenDone = false;
                }
            }
        }

        private void RightSharpeningMethod(Item item, float distance, float speed)
        {
            if (item.GetComponent<CollisionHandler>().isColliding)
            {
                foreach (CollisionInstance collisionInstance in item.GetComponent<CollisionHandler>().collisions)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart is RagdollPart part && (part.ragdoll.creature.handLeft == item.mainHandler.otherHand || part.ragdoll.creature.handRight == item.mainHandler.otherHand) && !rightCollidingWithOtherHand)
                        {
                            rightCollidingWithOtherHand = true;
                            rightPositionOnSwordStart = item.mainHandler.otherHand.transform.position;
                        }
                    }
                    // Colliding with another item
                    else
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.item?.mainHandler == item.mainHandler.otherHand && !rightCollidingWithOtherHand)
                        {
                            rightCollidingWithOtherHand = true;
                            rightPositionOnSwordStart = item.mainHandler.otherHand.grabbedHandle.item.transform.position;
                        }
                    }
                }
                if (rightCollidingWithOtherHand && !rightSharpenDone)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (Vector3.Distance(item.mainHandler.otherHand.transform.position, rightPositionOnSwordStart) > distance && Vector3.Dot(item.mainHandler.otherHand.Velocity(), item.rb.velocity) < -speed)
                        {
                            //Activate Imbue
                            item.UnImbueItem();
                            if (item.mainHandler.caster.spellInstance != null)
                                item.ImbueItem(item.mainHandler.caster.spellInstance.id);
                            rightSharpenDone = true;
                        }
                    }
                    // Colliding with another item
                    else
                    {
                        if (Vector3.Distance(item.mainHandler.otherHand.grabbedHandle.item.transform.position, rightPositionOnSwordStart) > distance && Vector3.Dot(item.mainHandler.otherHand.grabbedHandle.item.rb.velocity, item.rb.velocity) < -speed)
                        {
                            //Activate Imbue
                            item.UnImbueItem();
                            if (item.mainHandler.caster.spellInstance != null)
                                item.ImbueItem(item.mainHandler.caster.spellInstance.id);
                            if (item.mainHandler.otherHand.caster.spellInstance != null && item.mainHandler.otherHand.playerHand.controlHand.alternateUsePressed)
                            {
                                item.mainHandler.otherHand.grabbedHandle.item.UnImbueItem();
                                if (item.mainHandler.otherHand.caster.spellInstance != null)
                                    item.mainHandler.otherHand.grabbedHandle.item.ImbueItem(item.mainHandler.otherHand.caster.spellInstance.id);
                            }
                            rightSharpenDone = true;
                        }
                    }
                }
            }
            else
            {
                if (rightCollidingWithOtherHand)
                {
                    rightCollidingWithOtherHand = false;
                    rightPositionOnSwordStart = Vector3.zero;
                    rightSharpenDone = false;
                }
            }
        }
    }
}
