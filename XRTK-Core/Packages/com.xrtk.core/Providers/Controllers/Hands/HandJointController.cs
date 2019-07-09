﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Services;

namespace XRTK.Providers.Controllers.Hands
{
    public class HandJointController : BaseControllerDataProvider, IMixedRealityHandJointService
    {
        private IMixedRealityHandController leftHand;
        private IMixedRealityHandController rightHand;

        private Dictionary<TrackedHandJoint, Transform> leftHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();
        private Dictionary<TrackedHandJoint, Transform> rightHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();

        #region BaseInputDeviceManager Implementation

        public HandJointController(
            IMixedRealityInputSystem inputSystem,
            string name,
            uint priority,
            BaseMixedRealityControllerDataProviderProfile profile)
            : base(name, priority, profile) { }

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
        public override void Disable()
        {
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

        #endregion BaseInputDeviceManager Implementation

        #region IMixedRealityHandJointService Implementation

        public Transform RequestJointTransform(TrackedHandJoint joint, Handedness handedness)
        {
            IMixedRealityHandController hand = null;
            Dictionary<TrackedHandJoint, Transform> fauxJoints = null;
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
                return null;
            }

            Transform jointTransform = null;
            if (fauxJoints != null && !fauxJoints.TryGetValue(joint, out jointTransform))
            {
                jointTransform = new GameObject().transform;
                // Since this service survives scene loading and unloading, the fauxJoints it manages need to as well.
                Object.DontDestroyOnLoad(jointTransform.gameObject);
                jointTransform.name = string.Format("Joint Tracker: {1} {0}", joint, handedness);

                if (hand != null && hand.TryGetJoint(joint, out MixedRealityPose pose))
                {
                    jointTransform.SetPositionAndRotation(pose.Position, pose.Rotation);
                }

                fauxJoints.Add(joint, jointTransform);
            }

            return jointTransform;
        }

        public bool IsHandTracked(Handedness handedness)
        {
            return handedness == Handedness.Left ? leftHand != null : handedness == Handedness.Right ? rightHand != null : false;
        }

        #endregion IMixedRealityHandJointService Implementation
    }
}
