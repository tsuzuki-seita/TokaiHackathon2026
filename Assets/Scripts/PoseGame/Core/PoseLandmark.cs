namespace PoseGame
{
    // BlazePose 33-keypoint layout (MediaPipe Pose convention).
    public enum PoseLandmark
    {
        Nose = 0,
        LeftEyeInner = 1,
        LeftEye = 2,
        LeftEyeOuter = 3,
        RightEyeInner = 4,
        RightEye = 5,
        RightEyeOuter = 6,
        LeftEar = 7,
        RightEar = 8,
        MouthLeft = 9,
        MouthRight = 10,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftElbow = 13,
        RightElbow = 14,
        LeftWrist = 15,
        RightWrist = 16,
        LeftPinky = 17,
        RightPinky = 18,
        LeftIndex = 19,
        RightIndex = 20,
        LeftThumb = 21,
        RightThumb = 22,
        LeftHip = 23,
        RightHip = 24,
        LeftKnee = 25,
        RightKnee = 26,
        LeftAnkle = 27,
        RightAnkle = 28,
        LeftHeel = 29,
        RightHeel = 30,
        LeftFootIndex = 31,
        RightFootIndex = 32,
    }

    public static class PoseSkeleton
    {
        public const int KeypointCount = 33;

        // Bone connections used for skeleton line drawing (kept to torso + limbs for legibility).
        public static readonly (PoseLandmark, PoseLandmark)[] Bones =
        {
            (PoseLandmark.LeftShoulder, PoseLandmark.RightShoulder),
            (PoseLandmark.LeftShoulder, PoseLandmark.LeftElbow),
            (PoseLandmark.LeftElbow, PoseLandmark.LeftWrist),
            (PoseLandmark.RightShoulder, PoseLandmark.RightElbow),
            (PoseLandmark.RightElbow, PoseLandmark.RightWrist),
            (PoseLandmark.LeftShoulder, PoseLandmark.LeftHip),
            (PoseLandmark.RightShoulder, PoseLandmark.RightHip),
            (PoseLandmark.LeftHip, PoseLandmark.RightHip),
            (PoseLandmark.LeftHip, PoseLandmark.LeftKnee),
            (PoseLandmark.LeftKnee, PoseLandmark.LeftAnkle),
            (PoseLandmark.RightHip, PoseLandmark.RightKnee),
            (PoseLandmark.RightKnee, PoseLandmark.RightAnkle),
            (PoseLandmark.Nose, PoseLandmark.LeftShoulder),
            (PoseLandmark.Nose, PoseLandmark.RightShoulder),
        };
    }
}
