using System.Collections.Generic;

namespace PoseGame
{
    // One joint-angle target: the interior angle at `Vertex`, formed by the rays to `From` and `To`.
    public struct AngleTarget
    {
        public PoseLandmark From;
        public PoseLandmark Vertex;
        public PoseLandmark To;
        public float TargetDegrees;
        public float ToleranceDegrees;
        public float Weight;

        public AngleTarget(PoseLandmark from, PoseLandmark vertex, PoseLandmark to, float targetDegrees, float toleranceDegrees = 40f, float weight = 1f)
        {
            From = from;
            Vertex = vertex;
            To = to;
            TargetDegrees = targetDegrees;
            ToleranceDegrees = toleranceDegrees;
            Weight = weight;
        }
    }

    public class PoseTheme
    {
        public string Id;
        public string DisplayName;
        public string Emoji;
        public AngleTarget[] Targets;

        public PoseTheme(string id, string displayName, string emoji, AngleTarget[] targets)
        {
            Id = id;
            DisplayName = displayName;
            Emoji = emoji;
            Targets = targets;
        }
    }

    // Built-in "お題" (theme) library. Target angles are hand-authored approximations of each
    // pose's silhouette as seen from a front-facing webcam; scoring uses a generous tolerance
    // per joint so exact biomechanical precision isn't required for a good match.
    public static class PoseThemeLibrary
    {
        const float ArmWeight = 1.2f;
        const float LegWeight = 0.8f;

        static AngleTarget LS(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.LeftHip, PoseLandmark.LeftShoulder, PoseLandmark.LeftElbow, deg, tol, ArmWeight);
        static AngleTarget RS(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.RightHip, PoseLandmark.RightShoulder, PoseLandmark.RightElbow, deg, tol, ArmWeight);
        static AngleTarget LE(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.LeftShoulder, PoseLandmark.LeftElbow, PoseLandmark.LeftWrist, deg, tol, ArmWeight);
        static AngleTarget RE(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.RightShoulder, PoseLandmark.RightElbow, PoseLandmark.RightWrist, deg, tol, ArmWeight);
        static AngleTarget LH(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.LeftShoulder, PoseLandmark.LeftHip, PoseLandmark.LeftKnee, deg, tol, LegWeight);
        static AngleTarget RH(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.RightShoulder, PoseLandmark.RightHip, PoseLandmark.RightKnee, deg, tol, LegWeight);
        static AngleTarget LK(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.LeftHip, PoseLandmark.LeftKnee, PoseLandmark.LeftAnkle, deg, tol, LegWeight);
        static AngleTarget RK(float deg, float tol = 40f) => new AngleTarget(PoseLandmark.RightHip, PoseLandmark.RightKnee, PoseLandmark.RightAnkle, deg, tol, LegWeight);

        public static readonly List<PoseTheme> Themes = new List<PoseTheme>
        {
            new PoseTheme("t_pose", "Tポーズ", "🤸", new[]
            {
                LS(90), RS(90), LE(175), RE(175), LH(175), RH(175), LK(175), RK(175),
            }),
            new PoseTheme("banzai", "バンザイ", "🙌", new[]
            {
                LS(170), RS(170), LE(175), RE(175), LH(175), RH(175), LK(175), RK(175),
            }),
            new PoseTheme("ninja", "忍者ポーズ", "🥷", new[]
            {
                LS(40), RS(40), LE(60), RE(60), LH(140), RH(140), LK(130), RK(130),
            }),
        };
    }
}
