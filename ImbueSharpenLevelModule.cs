using ThunderRoad;
using UnityEngine;

namespace ImbueSharpen
{
    public class ImbueSharpenLevelModule : ThunderScript
    {
        private Item rightHandItem;
        private Item leftHandItem;
        private bool leftCollidingWithOtherHand = false;
        private Vector3 leftPositionOnSwordStart = Vector3.zero;
        private bool leftSharpenDone = false;
        private bool rightCollidingWithOtherHand = false;
        private Vector3 rightPositionOnSwordStart = Vector3.zero;
        private bool rightSharpenDone = false;

        public enum ButtonPress
        {
            Alternate,
            Trigger,
            Grip
        }

        [ModOptionCategory("Button Press Options", 1)]
        [ModOption(name: "Button Press", tooltip: "Select which button is pressed to allow the imbue sharpening",
            saveValue = true, order = 1)]
        public static ButtonPress buttonPress;

        public override void ScriptEnable()
        {
            EventManager.onPossess += EventManager_onPossess;
            EventManager.onUnpossess += EventManager_onUnpossess;
            base.ScriptEnable();
        }

        public override void ScriptDisable()
        {
            EventManager.onPossess -= EventManager_onPossess;
            EventManager.onUnpossess -= EventManager_onUnpossess;
            base.ScriptDisable();
        }

        private void EventManager_onUnpossess(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            creature.handLeft.OnGrabEvent -= HandLeft_OnGrabEvent;
            creature.handLeft.OnUnGrabEvent -= HandLeft_OnUnGrabEvent;
            creature.handRight.OnGrabEvent -= HandRight_OnGrabEvent;
            creature.handRight.OnUnGrabEvent -= HandRight_OnUnGrabEvent;
        }

        private void EventManager_onPossess(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            creature.handLeft.OnGrabEvent += HandLeft_OnGrabEvent;
            creature.handLeft.OnUnGrabEvent += HandLeft_OnUnGrabEvent;
            creature.handRight.OnGrabEvent += HandRight_OnGrabEvent;
            creature.handRight.OnUnGrabEvent += HandRight_OnUnGrabEvent;
        }

        private void HandRight_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd) return;
            rightHandItem = null;
        }

        private void HandRight_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation,
            EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            rightHandItem = handle.item;
        }

        private void HandLeft_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd) return;
            leftHandItem = null;
        }

        private void HandLeft_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation,
            EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            leftHandItem = handle.item;
        }

        private void CheckItem(Item item, bool isLeft)
        {
            if (item == null) return;
            if (!item.mainHandler.IsPlayer()) return;
            if ((!item.mainHandler.playerHand.controlHand.alternateUsePressed ||
                 buttonPress != ButtonPress.Alternate)
                && (!item.mainHandler.playerHand.controlHand.gripPressed ||
                    buttonPress != ButtonPress.Grip)
                && (!item.mainHandler.playerHand.controlHand.castPressed ||
                    buttonPress != ButtonPress.Trigger)) return;
            if (isLeft)
                LeftSharpeningMethod(item, 0.1f, 0.3f);
            else
                RightSharpeningMethod(item, 0.1f, 0.3f);
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            CheckItem(leftHandItem, true);
            CheckItem(rightHandItem, false);
        }

        private void LeftSharpeningMethod(Item item, float distance, float speed)
        {
            if (item.mainCollisionHandler.isColliding)
            {
                foreach (CollisionInstance collisionInstance in item.mainCollisionHandler.collisions)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart is not RagdollPart part ||
                            (part.ragdoll.creature.handLeft != item.mainHandler.otherHand &&
                             part.ragdoll.creature.handRight != item.mainHandler.otherHand) ||
                            leftCollidingWithOtherHand) continue;
                        leftCollidingWithOtherHand = true;
                        leftPositionOnSwordStart = item.mainHandler.otherHand.transform.position;
                    }
                    // Colliding with another item
                    else
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.item?.mainHandler !=
                            item.mainHandler.otherHand || leftCollidingWithOtherHand) continue;
                        leftCollidingWithOtherHand = true;
                        leftPositionOnSwordStart = item.mainHandler.otherHand.grabbedHandle.item.transform.position;
                    }
                }
                if (!leftCollidingWithOtherHand || leftSharpenDone) return;
                // Colliding with the hand
                if (item.mainHandler.otherHand.grabbedHandle == null)
                {
                    if (!(Vector3.Distance(item.mainHandler.otherHand.transform.position,
                              leftPositionOnSwordStart) >
                          distance) || !(Vector3.Dot(item.mainHandler.otherHand.Velocity(),
                            item.physicBody.rigidBody.velocity) < -speed)) return;
                    //Activate Imbue
                    item.UnImbueItem();
                    if (item.mainHandler.caster.spellInstance != null)
                        item.ImbueItem(item.mainHandler.caster);
                }
                // Colliding with another item
                else
                {
                    if (!(Vector3.Distance(item.mainHandler.otherHand.grabbedHandle.item.transform.position,
                            leftPositionOnSwordStart) > distance) ||
                        !(Vector3.Dot(item.mainHandler.otherHand.grabbedHandle.item.physicBody.rigidBody.velocity,
                            item.physicBody.rigidBody.velocity) < -speed)) return;
                    //Activate Imbue
                    item.UnImbueItem();
                    if (item.mainHandler.caster.spellInstance != null)
                        item.ImbueItem(item.mainHandler.caster);
                    if (item.mainHandler.otherHand.caster.spellInstance != null
                        && (item.mainHandler.otherHand.playerHand.controlHand.alternateUsePressed && buttonPress == ButtonPress.Alternate
                            || item.mainHandler.otherHand.playerHand.controlHand.gripPressed && buttonPress == ButtonPress.Grip
                            || item.mainHandler.otherHand.playerHand.controlHand.castPressed && buttonPress == ButtonPress.Trigger))
                    {
                        item.mainHandler.otherHand.grabbedHandle.item.UnImbueItem();
                        if (item.mainHandler.otherHand.caster.spellInstance != null)
                            item.mainHandler.otherHand.grabbedHandle.item.ImbueItem(item.mainHandler.otherHand.caster);
                    }
                }
                leftSharpenDone = true;
            }
            else
            {
                if (!leftCollidingWithOtherHand) return;
                leftCollidingWithOtherHand = false;
                leftPositionOnSwordStart = Vector3.zero;
                leftSharpenDone = false;
            }
        }

        private void RightSharpeningMethod(Item item, float distance, float speed)
        {
            if (item.mainCollisionHandler.isColliding)
            {
                foreach (CollisionInstance collisionInstance in item.mainCollisionHandler.collisions)
                {
                    // Colliding with the hand
                    if (item.mainHandler.otherHand.grabbedHandle == null)
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart is not RagdollPart part ||
                            (part.ragdoll.creature.handLeft != item.mainHandler.otherHand &&
                             part.ragdoll.creature.handRight != item.mainHandler.otherHand) ||
                            rightCollidingWithOtherHand) continue;
                        rightCollidingWithOtherHand = true;
                        rightPositionOnSwordStart = item.mainHandler.otherHand.transform.position;
                    }
                    // Colliding with another item
                    else
                    {
                        if (collisionInstance.targetColliderGroup?.collisionHandler?.item?.mainHandler !=
                            item.mainHandler.otherHand || rightCollidingWithOtherHand) continue;
                        rightCollidingWithOtherHand = true;
                        rightPositionOnSwordStart =
                            item.mainHandler.otherHand.grabbedHandle.item.transform.position;
                    }
                }
                if (!rightCollidingWithOtherHand || rightSharpenDone) return;
                // Colliding with the hand
                if (item.mainHandler.otherHand.grabbedHandle == null)
                {
                    if (!(Vector3.Distance(item.mainHandler.otherHand.transform.position, rightPositionOnSwordStart) >
                          distance) || !(Vector3.Dot(item.mainHandler.otherHand.Velocity(),
                            item.physicBody.rigidBody.velocity) < -speed)) return;
                    //Activate Imbue
                    item.UnImbueItem();
                    if (item.mainHandler.caster.spellInstance != null)
                        item.ImbueItem(item.mainHandler.caster);
                }
                // Colliding with another item
                else
                {
                    if (!(Vector3.Distance(item.mainHandler.otherHand.grabbedHandle.item.transform.position,
                            rightPositionOnSwordStart) > distance) ||
                        !(Vector3.Dot(item.mainHandler.otherHand.grabbedHandle.item.physicBody.rigidBody.velocity,
                            item.physicBody.rigidBody.velocity) < -speed)) return;
                    //Activate Imbue
                    item.UnImbueItem();
                    if (item.mainHandler.caster.spellInstance != null)
                        item.ImbueItem(item.mainHandler.caster);
                    if (item.mainHandler.otherHand.caster.spellInstance != null
                        && (item.mainHandler.otherHand.playerHand.controlHand.alternateUsePressed && buttonPress == ButtonPress.Alternate 
                            || item.mainHandler.otherHand.playerHand.controlHand.gripPressed && buttonPress == ButtonPress.Grip
                            || item.mainHandler.otherHand.playerHand.controlHand.castPressed && buttonPress == ButtonPress.Trigger))
                    {
                        item.mainHandler.otherHand.grabbedHandle.item.UnImbueItem();
                        if (item.mainHandler.otherHand.caster.spellInstance != null)
                            item.mainHandler.otherHand.grabbedHandle.item.ImbueItem(item.mainHandler.otherHand.caster);
                    }
                }
                rightSharpenDone = true;
            }
            else
            {
                if (!rightCollidingWithOtherHand) return;
                rightCollidingWithOtherHand = false;
                rightPositionOnSwordStart = Vector3.zero;
                rightSharpenDone = false;
            }
        }
    }
}