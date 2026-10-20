using System;
using UnityEngine;

namespace TARS.Locomotion
{
    public static class LegIK
    {
        private const float SqrEpsilon = 1e-8f;
        private const float MaxJointBend = 0.95f * Mathf.PI;
        private const int CurlSamples = 64;
        private const int MaxCurlIterations = 12;
        private const float CurlTolerance = 1e-5f;

        public static void Capture(ReadOnlySpan<Transform> bones, Vector3 hint, Span<Vector3> axes, Span<Vector3> normals, Span<float> restBends)
        {
            Vector3 root = bones[0].position;
            Vector3 direction = (bones[bones.Length - 1].position - root).normalized;
            Vector3 bendDirection = Vector3.ProjectOnPlane(hint - root, direction);
            if (bendDirection.sqrMagnitude < SqrEpsilon)
                bendDirection = Vector3.ProjectOnPlane(bones[1].position - root, direction);
            Vector3 bendNormal = Vector3.Cross(bendDirection, direction).normalized;
            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 segment = bones[i + 1].position - bones[i].position;
                Quaternion inverse = Quaternion.Inverse(bones[i].rotation);
                axes[i] = inverse * segment.normalized;
                normals[i] = inverse * bendNormal;
                if (i < restBends.Length)
                    restBends[i] = Vector3.SignedAngle(segment, bones[i + 2].position - bones[i + 1].position, bendNormal) * Mathf.Deg2Rad;
            }
        }

        public static void MeasureCurl(ReadOnlySpan<float> lengths, ReadOnlySpan<float> weights, float chainLength, out float curlLimit, out float minReach)
        {
            curlLimit = 0f;
            minReach = chainLength;
            float maxWeight = 0f;
            for (int i = 0; i < lengths.Length - 1; i++)
                maxWeight = Mathf.Max(maxWeight, Mathf.Abs(JointWeight(weights, i)));
            if (maxWeight <= 0f) return;
            float range = MaxJointBend / maxWeight;
            for (int i = 1; i <= CurlSamples; i++)
            {
                float curl = range * i / CurlSamples;
                float distance = ChainEnd(lengths, weights, curl, out _).magnitude;
                if (distance >= minReach) break;
                minReach = distance;
                curlLimit = curl;
            }
        }

        public static float Solve(Vector3 root, ReadOnlySpan<float> lengths, ReadOnlySpan<Vector3> axes, ReadOnlySpan<Vector3> normals, ReadOnlySpan<float> weights, float chainLength, float minReach, float curlLimit, float curl, Vector3 target, Vector3 hint, float softness, Span<Quaternion> rotations, Span<Vector3> positions)
        {
            Vector3 toTarget = target - root;
            float targetDistance = toTarget.magnitude;
            if (targetDistance * targetDistance < SqrEpsilon) return curl;
            Vector3 direction = toTarget / targetDistance;
            Vector3 bendDirection = Vector3.ProjectOnPlane(hint - root, direction);
            if (bendDirection.sqrMagnitude < SqrEpsilon)
                bendDirection = Vector3.ProjectOnPlane(positions[1] - root, direction);
            if (bendDirection.sqrMagnitude < SqrEpsilon) return curl;
            bendDirection.Normalize();
            Vector3 bendNormal = Vector3.Cross(bendDirection, direction);
            float reach = SoftReach(Mathf.Max(targetDistance, minReach), chainLength, softness);
            if (lengths.Length == 2)
            {
                float upperLength = lengths[0];
                float lowerLength = lengths[1];
                float cosine = Mathf.Clamp((upperLength * upperLength + reach * reach - lowerLength * lowerLength) / (2f * upperLength * reach), -1f, 1f);
                Vector3 knee = root + (direction * cosine + bendDirection * Mathf.Sqrt(1f - cosine * cosine)) * upperLength;
                Vector3 end = root + direction * reach;
                rotations[0] = AlignFrame(knee - root, bendNormal, axes[0], normals[0]);
                rotations[1] = AlignFrame(end - knee, bendNormal, axes[1], normals[1]);
                positions[0] = root;
                positions[1] = knee;
                positions[2] = end;
                return 0f;
            }
            curl = SolveCurl(lengths, weights, chainLength, minReach, curlLimit, curl, reach);
            Vector2 chainEnd = ChainEnd(lengths, weights, curl, out _);
            float angle = -Mathf.Atan2(chainEnd.y, chainEnd.x);
            Vector3 position = root;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (i > 0)
                    angle -= JointWeight(weights, i - 1) * curl;
                Vector3 segment = direction * Mathf.Cos(angle) + bendDirection * Mathf.Sin(angle);
                rotations[i] = AlignFrame(segment, bendNormal, axes[i], normals[i]);
                positions[i] = position;
                position += segment * lengths[i];
            }
            positions[lengths.Length] = position;
            return curl;
        }

        private static float SolveCurl(ReadOnlySpan<float> lengths, ReadOnlySpan<float> weights, float chainLength, float minReach, float curlLimit, float guess, float reach)
        {
            if (reach >= chainLength) return 0f;
            if (reach <= minReach) return curlLimit;
            float low = 0f;
            float high = curlLimit;
            float curl = Mathf.Clamp(guess, low, high);
            float tolerance = CurlTolerance * chainLength;
            for (int i = 0; i < MaxCurlIterations; i++)
            {
                Vector2 end = ChainEnd(lengths, weights, curl, out Vector2 slope);
                float distance = end.magnitude;
                float error = distance - reach;
                if (Mathf.Abs(error) < tolerance) break;
                if (error > 0f)
                    low = curl;
                else
                    high = curl;
                float derivative = Vector2.Dot(end, slope) / distance;
                float next = derivative < 0f ? curl - error / derivative : low;
                curl = next > low && next < high ? next : 0.5f * (low + high);
            }
            return curl;
        }

        private static Vector2 ChainEnd(ReadOnlySpan<float> lengths, ReadOnlySpan<float> weights, float curl, out Vector2 slope)
        {
            float angle = 0f;
            float rate = 0f;
            Vector2 end = Vector2.zero;
            slope = Vector2.zero;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (i > 0)
                {
                    float weight = JointWeight(weights, i - 1);
                    angle -= weight * curl;
                    rate -= weight;
                }
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                end += new Vector2(cosine, sine) * lengths[i];
                slope += new Vector2(-sine, cosine) * (lengths[i] * rate);
            }
            return end;
        }

        private static float JointWeight(ReadOnlySpan<float> weights, int joint)
        {
            return joint < weights.Length ? weights[joint] : 1f;
        }

        private static float SoftReach(float distance, float chainLength, float softness)
        {
            float softRange = chainLength * softness;
            float softStart = chainLength - softRange;
            if (distance <= softStart) return distance;
            if (softRange * softRange < SqrEpsilon) return chainLength;
            return softStart + softRange * (1f - Mathf.Exp(-(distance - softStart) / softRange));
        }

        private static Quaternion AlignFrame(Vector3 axis, Vector3 bendNormal, Vector3 localAxis, Vector3 localBendNormal)
        {
            return Quaternion.LookRotation(axis, bendNormal) * Quaternion.Inverse(Quaternion.LookRotation(localAxis, localBendNormal));
        }
    }
}
