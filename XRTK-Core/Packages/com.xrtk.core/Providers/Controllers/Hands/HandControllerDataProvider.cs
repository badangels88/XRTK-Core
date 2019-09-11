﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using XRTK.Definitions.Controllers.Hands;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Services;

namespace XRTK.Providers.Controllers.Hands
{
    public class HandControllerDataProvider : BaseControllerDataProvider, IMixedRealityHandControllerDataProvider
    {
        private HandControllerDataProviderProfile profile;

        // Joint / Mesh update events
        private InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> jointPoseInputEventData;
        private InputEventData<HandMeshUpdatedEventData> handMeshInputEventData;
        private List<IMixedRealityHandJointHandler> handJointUpdatedEventListeners = new List<IMixedRealityHandJointHandler>();
        private List<IMixedRealityHandMeshHandler> handMeshUpdatedEventListeners = new List<IMixedRealityHandMeshHandler>();

        private IMixedRealityHandController leftHand;
        private IMixedRealityHandController rightHand;

        private Dictionary<TrackedHandJoint, Transform> leftHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();
        private Dictionary<TrackedHandJoint, Transform> rightHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();

        /// <inheritdoc />
        public IReadOnlyList<IMixedRealityHandJointHandler> HandJointUpdatedEventHandlers => handJointUpdatedEventListeners;

        /// <inheritdoc />
        public IReadOnlyList<IMixedRealityHandMeshHandler> HandMeshUpdatedEventHandlers => handMeshUpdatedEventListeners;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="profile"></param>
        public HandControllerDataProvider(string name, uint priority, HandControllerDataProviderProfile profile)
            : base(name, priority, profile)
        {
            this.profile = profile;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            jointPoseInputEventData = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>>(EventSystem.current);
            handMeshInputEventData = new InputEventData<HandMeshUpdatedEventData>(EventSystem.current);
        }


        /// <inheritdoc />
        public override void Enable()
        {
            for (int i = 0; i < profile.RegisteredControllerDataProviders.Length; i++)
            {
                HandControllerDataProviderConfiguration controllerDataProvider = profile.RegisteredControllerDataProviders[i];
                if (!MixedRealityToolkit.CreateAndRegisterService<IMixedRealityPlatformHandControllerDataProvider>(
                                    controllerDataProvider.DataProviderType,
                                    controllerDataProvider.RuntimePlatform,
                                    controllerDataProvider.DataProviderName,
                                    controllerDataProvider.Priority,
                                    controllerDataProvider.Profile))
                {
                    Debug.LogError($"Failed to start {controllerDataProvider.DataProviderName}!");
                }
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            MixedRealityToolkit.UnregisterServicesOfType<IMixedRealityPlatformHandControllerDataProvider>();

            // Check existence of fauxJoints before destroying. This avoids a (harmless) race
            // condition when the service is getting destroyed at the same time that the gameObjects
            // are being destroyed at shutdown.
            if (leftHandFauxJoints != null)
            {
                foreach (var fauxJoint in leftHandFauxJoints.Values)
                {
                    if (fauxJoint != null)
                    {
                        Object.Destroy(fauxJoint.gameObject);
                    }
                }

                leftHandFauxJoints.Clear();
            }

            if (rightHandFauxJoints != null)
            {
                foreach (var fauxJoint in rightHandFauxJoints.Values)
                {
                    if (fauxJoint != null)
                    {
                        Object.Destroy(fauxJoint.gameObject);
                    }
                }

                rightHandFauxJoints.Clear();
            }
        }

        /// <inheritdoc />
        public override void LateUpdate()
        {
            leftHand = null;
            rightHand = null;

            foreach (var detectedController in MixedRealityToolkit.InputSystem.DetectedControllers)
            {
                var hand = detectedController as IMixedRealityHandController;
                if (hand != null)
                {
                    if (detectedController.ControllerHandedness == Handedness.Left)
                    {
                        if (leftHand == null)
                        {
                            leftHand = hand;
                        }
                    }
                    else if (detectedController.ControllerHandedness == Handedness.Right)
                    {
                        if (rightHand == null)
                        {
                            rightHand = hand;
                        }
                    }
                }
            }

            if (leftHand != null)
            {
                foreach (var fauxJoint in leftHandFauxJoints)
                {
                    if (leftHand.TryGetJoint(fauxJoint.Key, out MixedRealityPose pose))
                    {
                        fauxJoint.Value.SetPositionAndRotation(pose.Position, pose.Rotation);
                    }
                }
            }

            if (rightHand != null)
            {
                foreach (var fauxJoint in rightHandFauxJoints)
                {
                    if (rightHand.TryGetJoint(fauxJoint.Key, out MixedRealityPose pose))
                    {
                        fauxJoint.Value.SetPositionAndRotation(pose.Position, pose.Rotation);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Register(IMixedRealityHandJointHandler handler)
        {
            if (handler != null)
            {
                handJointUpdatedEventListeners.Add(handler);
            }
        }

        /// <inheritdoc />
        public void Unregister(IMixedRealityHandJointHandler handler)
        {
            if (handJointUpdatedEventListeners.Contains(handler))
            {
                handJointUpdatedEventListeners.Remove(handler);
            }
        }

        /// <inheritdoc />
        public void Register(IMixedRealityHandMeshHandler handler)
        {
            if (handler != null)
            {
                handMeshUpdatedEventListeners.Add(handler);
            }
        }

        /// <inheritdoc />
        public void Unregister(IMixedRealityHandMeshHandler handler)
        {
            if (handMeshUpdatedEventListeners.Contains(handler))
            {
                handMeshUpdatedEventListeners.Remove(handler);
            }
        }

        /// <inheritdoc />
        public void UpdateHandJoints(IMixedRealityInputSource source, Handedness handedness, IDictionary<TrackedHandJoint, MixedRealityPose> jointPoses)
        {
            jointPoseInputEventData.Initialize(source, handedness, MixedRealityInputAction.None, jointPoses);
            for (int i = 0; i < HandJointUpdatedEventHandlers.Count; i++)
            {
                HandJointUpdatedEventHandlers[i].OnJointUpdated(jointPoseInputEventData);
            }
        }

        /// <inheritdoc />
        public void UpdateHandMesh(IMixedRealityInputSource source, Handedness handedness, HandMeshUpdatedEventData handMeshInfo)
        {
            handMeshInputEventData.Initialize(source, handedness, MixedRealityInputAction.None, handMeshInfo);
            for (int i = 0; i < HandMeshUpdatedEventHandlers.Count; i++)
            {
                HandMeshUpdatedEventHandlers[i].OnMeshUpdated(handMeshInputEventData);
            }
        }

        /// <inheritdoc />
        public bool TryGetJointTransform(TrackedHandJoint joint, Handedness handedness, out Transform jointTransform)
        {
            Dictionary<TrackedHandJoint, Transform> fauxJoints;
            IMixedRealityHandController hand;

            if (handedness == Handedness.Left)
            {
                hand = leftHand;
                fauxJoints = leftHandFauxJoints;
            }
            else if (handedness == Handedness.Right)
            {
                hand = rightHand;
                fauxJoints = rightHandFauxJoints;
            }
            else
            {
                jointTransform = null;
                return false;
            }

            if (fauxJoints != null && fauxJoints.TryGetValue(joint, out Transform existingJointTransform))
            {
                jointTransform = existingJointTransform;
                return true;
            }

            Transform newJointTransform = new GameObject().transform;
            newJointTransform.name = $"Joint Tracker: {joint} {handedness}";

            // Since this service survives scene loading and unloading, the fauxJoints it manages need to as well.
            Object.DontDestroyOnLoad(newJointTransform.gameObject);

            if (hand != null && hand.TryGetJoint(joint, out MixedRealityPose pose))
            {
                newJointTransform.SetPositionAndRotation(pose.Position, pose.Rotation);
            }

            fauxJoints.Add(joint, newJointTransform);
            jointTransform = newJointTransform;
            return true;
        }

        /// <inheritdoc />
        public bool IsHandTracked(Handedness handedness)
        {
            switch (handedness)
            {
                case Handedness.None:
                    return leftHand == null && rightHand == null;
                case Handedness.Left:
                    return leftHand != null;
                case Handedness.Right:
                    return rightHand != null;
                case Handedness.Both:
                    return leftHand != null && rightHand != null;
                case Handedness.Any:
                    return leftHand != null || rightHand != null;
                case Handedness.Other:
                default:
                    return false;
            }
        }
    }
}