using UnityEngine;

namespace PoseGame
{
    // A single detected person's pose, in normalized [0,1] space of the FULL camera frame
    // (i.e. zone-local coordinates already remapped into full-frame space).
    public class PersonPose
    {
        public bool IsDetected;
        public float DetectionScore;
        public readonly Vector2[] Positions = new Vector2[PoseSkeleton.KeypointCount];
        public readonly float[] Visibility = new float[PoseSkeleton.KeypointCount];

        // width/height of the texture this pose was estimated from. Positions.x and .y are
        // both normalized 0..1 against their own axis, so angle math needs this to correct for
        // non-square source textures (otherwise a right angle in real life reads as skewed).
        public float Aspect = 1f;

        public bool IsVisible(PoseLandmark landmark, float threshold = 0.5f)
        {
            return Visibility[(int)landmark] >= threshold;
        }

        public Vector2 Get(PoseLandmark landmark) => Positions[(int)landmark];

        public void Reset()
        {
            IsDetected = false;
            DetectionScore = 0f;
        }
    }
}
