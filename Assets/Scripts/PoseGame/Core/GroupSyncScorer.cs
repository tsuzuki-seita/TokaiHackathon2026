using System.Collections.Generic;
using UnityEngine;

namespace PoseGame
{
    // Scores each player not against a fixed target pose, but against how closely their pose
    // matches everyone ELSE currently in frame — a "みんなで息を合わせる" cooperative sync score
    // rather than a vs-a-target competition. `jointSet` still supplies which joints to compare
    // and how strict to be (Weight / ToleranceDegrees); its TargetDegrees is unused here.
    public static class GroupSyncScorer
    {
        const float VisibilityThreshold = 0.4f;

        public static float[] Score(IReadOnlyList<PersonPose> poses, PoseTheme jointSet)
        {
            var count = poses.Count;
            var result = new float[count];

            var detected = new List<int>();
            for (var i = 0; i < count; i++)
                if (poses[i] != null && poses[i].IsDetected)
                    detected.Add(i);

            if (detected.Count < 2 || jointSet.Targets.Length == 0)
                return result; // need at least 2 people in frame to measure "matching everyone"

            var weightedSum = new float[count];
            var totalWeight = new float[count];
            var usableTargets = new int[count];
            var anglesByPlayer = new Dictionary<int, float>(detected.Count);

            foreach (var target in jointSet.Targets)
            {
                anglesByPlayer.Clear();

                foreach (var i in detected)
                {
                    var pose = poses[i];
                    if (!pose.IsVisible(target.From, VisibilityThreshold) ||
                        !pose.IsVisible(target.Vertex, VisibilityThreshold) ||
                        !pose.IsVisible(target.To, VisibilityThreshold))
                        continue;

                    var vertex = pose.Get(target.Vertex);
                    var from = pose.Get(target.From);
                    var to = pose.Get(target.To);
                    var a = new Vector2((from.x - vertex.x) * pose.Aspect, from.y - vertex.y);
                    var b = new Vector2((to.x - vertex.x) * pose.Aspect, to.y - vertex.y);
                    if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f)
                        continue;

                    anglesByPlayer[i] = Vector2.Angle(a, b);
                }

                // Need at least 2 people showing this joint to say anything about "syncing" on it.
                if (anglesByPlayer.Count < 2)
                    continue;

                var mean = 0f;
                foreach (var kv in anglesByPlayer)
                    mean += kv.Value;
                mean /= anglesByPlayer.Count;

                foreach (var kv in anglesByPlayer)
                {
                    var diff = Mathf.Abs(kv.Value - mean);
                    var subScore = Mathf.Clamp01(1f - diff / target.ToleranceDegrees);

                    weightedSum[kv.Key] += subScore * target.Weight;
                    totalWeight[kv.Key] += target.Weight;
                    usableTargets[kv.Key]++;
                }
            }

            var minUsable = Mathf.CeilToInt(jointSet.Targets.Length * 0.5f);
            foreach (var i in detected)
            {
                if (totalWeight[i] <= 0f || usableTargets[i] < minUsable)
                {
                    result[i] = 0f;
                    continue;
                }
                result[i] = 100f * weightedSum[i] / totalWeight[i];
            }

            return result;
        }
    }
}
