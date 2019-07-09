﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Utilities;

namespace XRTK.Providers.Controllers.Hands
{
    public class HandRay
    {
        // Constants from Shell Implementation of hand ray
        private const float DynamicPivotBaseY = -0.1f, DynamicPivotMultiplierY = 0.65f, DynamicPivotMinY = -0.6f, DynamicPivotMaxY = -0.2f;
        private const float DynamicPivotBaseX = 0.03f, DynamicPivotMultiplierX = 0.65f, DynamicPivotMinX = 0.08f, DynamicPivotMaxX = 0.15f;
        private const float HeadToPivotOffsetZ = 0.08f;
        private readonly float CursorBeamBackwardTolerance = 0.5f;
        private readonly float CursorBeamUpTolerance = 0.8f;

        // Smoothing factor for ray stabilization
        private const float StabilizedRayHalfLife = 0.01f;

        private StabilizedRay stabilizedRay = new StabilizedRay(StabilizedRayHalfLife);
        private Vector3 palmNormal;
        private Vector3 headForward;

        private Ray ray = new Ray();
        public Ray Ray
        {
            get
            {
                ray.origin = stabilizedRay.StabilizedPosition;
                ray.direction = stabilizedRay.StabilizedDirection;
                return ray;
            }
        }

        public bool ShouldShowRay
        {
            get
            {
                if (headForward.magnitude < Mathf.Epsilon)
                {
                    return false;
                }
                bool valid = true;
                if (CursorBeamBackwardTolerance >= 0)
                {
                    Vector3 cameraBackward = -headForward;
                    if (Vector3.Dot(palmNormal.normalized, cameraBackward) > CursorBeamBackwardTolerance)
                    {
                        valid = false;
                    }
                }
                if (valid && CursorBeamUpTolerance >= 0)
                {
                    if (Vector3.Dot(palmNormal, Vector3.up) > CursorBeamUpTolerance)
                    {
                        valid = false;
                    }
                }

                return valid;
            }
        }

        public void Update(Vector3 handPosition, Vector3 palmNormal, Transform headTransform, Handedness sourceHandedness)
        {
            Vector3 rayPivotPoint = ComputeRayPivotPosition(handPosition, headTransform, sourceHandedness);
            Vector3 measuredRayPosition = handPosition;
            Vector3 measuredDirection = measuredRayPosition - rayPivotPoint;
            this.palmNormal = palmNormal;
            this.headForward = headTransform.forward;

            stabilizedRay.AddSample(new Ray(measuredRayPosition, measuredDirection));
        }

        private Vector3 ComputeRayPivotPosition(Vector3 handPosition, Transform headTransform, Handedness sourceHandedness)
        {
            Vector3 handPositionHeadSpace = headTransform.InverseTransformPoint(handPosition);
            float relativePivotY = DynamicPivotBaseY + Mathf.Min(DynamicPivotMultiplierY * handPositionHeadSpace.y, 0);
            relativePivotY = Mathf.Clamp(relativePivotY, DynamicPivotMinY, DynamicPivotMaxY);

            float xBase = DynamicPivotBaseX;
            float xMultiplier = DynamicPivotMultiplierX;
            float xMin = DynamicPivotMinX;
            float xMax = DynamicPivotMaxX;
            if (sourceHandedness == Handedness.Left)
            {
                xBase = -xBase;
                float tmp = xMin;
                xMin = -xMax;
                xMax = tmp;
            }
            float relativePivotX = xBase + xMultiplier * handPositionHeadSpace.x;
            relativePivotX = Mathf.Clamp(relativePivotX, xMin, xMax);

            Vector3 relativePivot = new Vector3(
                    relativePivotX,
                    relativePivotY,
                    HeadToPivotOffsetZ
                );

            Quaternion headRotationFlat = Quaternion.Euler(0, headTransform.rotation.eulerAngles.y, 0);
            return headTransform.position + headRotationFlat * relativePivot;
        }
    }
}