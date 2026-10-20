using UnityEngine;

namespace TARS.Locomotion
{
    public struct LegState
    {
        public float PhaseOffset;
        public float DutyFactor;
        public float PreviousLegPhase;
        public float HipHeight;
        public float ChainLength;
        public Vector3 RestHipLocal;
        public Vector3 RestUpperLocal;
        public Vector3 RestFootLocal;
        public Vector3 RestForwardLocal;
        public Vector3 ProbeOrigin;
        public Vector3 FootPosition;
        public Quaternion FootRotation;
        public float FootPitch;
        public float HeelPitch;
        public float StanceReach;
        public bool ReachExhausted;
        public Vector3 AnklePosition;
        public Vector3 SupportAnkle;
        public Vector3 LiftOffPosition;
        public Vector3 LiftOffAnklePosition;
        public Quaternion LiftOffRotation;
        public float LiftOffPitch;
        public Quaternion LiftOffAnkle;
        public Quaternion RestAnkle;
        public Vector3 LandingAnklePosition;
        public Vector3 Placement;
        public Vector3 PlacementVelocity;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public bool TargetOnGround;
        public bool OnGround;
        public float SwingStartPhase;
        public float SwingProgress;
        public float SwingDuration;
        public bool IsSwinging;
        public bool WasInSwingWindow;

        public readonly float StanceProgress => PreviousLegPhase / DutyFactor;

        public readonly float ReachHeight(Vector3 ankleBodyPoint, Vector3 upperBodyPoint)
        {
            float horizontalSqr = (ankleBodyPoint.x - upperBodyPoint.x) * (ankleBodyPoint.x - upperBodyPoint.x) + (ankleBodyPoint.z - upperBodyPoint.z) * (ankleBodyPoint.z - upperBodyPoint.z);
            return ankleBodyPoint.y + Mathf.Sqrt(Mathf.Max(ChainLength * ChainLength - horizontalSqr, 0f)) - upperBodyPoint.y;
        }

        public readonly float HeelLiftAllowance(Vector3 footBodyPoint, Vector3 travelDirection, float alignment, LegRig leg, float maxPitch)
        {
            if (leg.ToeLength <= 0f || alignment <= 0f) return 0f;
            float trailing = (RestUpperLocal.x - footBodyPoint.x) * travelDirection.x + (RestUpperLocal.z - footBodyPoint.z) * travelDirection.z;
            return maxPitch * alignment * Mathf.Clamp01(trailing / leg.ToeLength);
        }
    }
}
