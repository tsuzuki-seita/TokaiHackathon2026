using Unity.Mathematics;
using Unity.Sentis;
using UnityEngine;

namespace PoseGame
{
    // Runs Google's BlazePose (detector + landmarker) via Sentis, synchronously, on a single
    // input texture, finding at most one person (the highest scoring detection). Adapted from
    // Unity's official Sentis BlazeDetection sample, ported to the Sentis 2.1.3 API available
    // on Unity 2022.3 LTS and made fully synchronous (no UnityEngine.Awaitable, which requires
    // Unity 2023.1+) so it can run from a plain Update loop.
    public class BlazePoseEstimator : System.IDisposable
    {
        const int k_NumAnchors = 2254;
        const int k_DetectorInputSize = 224;
        const int k_LandmarkerInputSize = 256;

        public float ScoreThreshold = 0.5f;

        readonly float[,] m_Anchors;
        readonly Worker m_DetectorWorker;
        readonly Worker m_LandmarkerWorker;
        readonly Tensor<float> m_DetectorInput;
        readonly Tensor<float> m_LandmarkerInput;

        public BlazePoseEstimator()
        {
            var anchorsCsv = Resources.Load<TextAsset>("PoseData/anchors");
            m_Anchors = BlazeUtils.LoadAnchors(anchorsCsv.text, k_NumAnchors);

            var detectorAsset = Resources.Load<ModelAsset>("PoseModels/pose_detection");
            var detectorModel = ModelLoader.Load(detectorAsset);

            var graph = new FunctionalGraph();
            var input = graph.AddInput(detectorModel, 0);
            var outputs = Functional.Forward(detectorModel, input);
            var boxes = outputs[0]; // (1, 2254, 12)
            var scores = outputs[1]; // (1, 2254, 1)
            var filtered = BlazeUtils.ArgMaxFiltering(boxes, scores);
            detectorModel = graph.Compile(filtered.Item1, filtered.Item2, filtered.Item3);

            m_DetectorWorker = new Worker(detectorModel, BackendType.GPUCompute);

            var landmarkerAsset = Resources.Load<ModelAsset>("PoseModels/pose_landmarks_lite");
            var landmarkerModel = ModelLoader.Load(landmarkerAsset);
            m_LandmarkerWorker = new Worker(landmarkerModel, BackendType.GPUCompute);

            m_DetectorInput = new Tensor<float>(new TensorShape(1, k_DetectorInputSize, k_DetectorInputSize, 3));
            m_LandmarkerInput = new Tensor<float>(new TensorShape(1, k_LandmarkerInputSize, k_LandmarkerInputSize, 3));
        }

        // Estimates a single pose from `texture`. Output positions are normalized [0,1]
        // in the texture's own space, bottom-left origin (matching Unity UI anchor / UV space).
        public bool Estimate(Texture texture, PersonPose outPose)
        {
            outPose.Reset();

            var textureWidth = texture.width;
            var textureHeight = texture.height;
            var size = Mathf.Max(textureWidth, textureHeight);

            var scale = size / (float)k_DetectorInputSize;
            var M = BlazeUtils.mul(
                BlazeUtils.TranslationMatrix(0.5f * (new float2(textureWidth, textureHeight) + new float2(-size, size))),
                BlazeUtils.ScaleMatrix(new float2(scale, -scale)));
            BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

            m_DetectorWorker.Schedule(m_DetectorInput);

            using var outputIdx = (m_DetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndClone();
            using var outputScore = (m_DetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndClone();
            using var outputBox = (m_DetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndClone();

            if (outputScore[0] < ScoreThreshold)
                return false;

            var idx = outputIdx[0];
            var anchorPosition = k_DetectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

            var kp1_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 4 + 0], outputBox[0, 0, 4 + 1]));
            var kp2_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 4 + 2], outputBox[0, 0, 4 + 3]));
            var delta_ImageSpace = kp2_ImageSpace - kp1_ImageSpace;
            var dscale = 1.25f;
            var radius = dscale * math.length(delta_ImageSpace);
            var theta = math.atan2(delta_ImageSpace.y, delta_ImageSpace.x);
            var origin2 = new float2(0.5f * k_LandmarkerInputSize, 0.5f * k_LandmarkerInputSize);
            var scale2 = radius / (0.5f * k_LandmarkerInputSize);
            var M2 = BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(
                BlazeUtils.TranslationMatrix(kp1_ImageSpace),
                BlazeUtils.ScaleMatrix(new float2(scale2, -scale2))),
                BlazeUtils.RotationMatrix(0.5f * Mathf.PI - theta)),
                BlazeUtils.TranslationMatrix(-origin2));
            BlazeUtils.SampleImageAffine(texture, m_LandmarkerInput, M2);

            m_LandmarkerWorker.Schedule(m_LandmarkerInput);

            using var landmarks = (m_LandmarkerWorker.PeekOutput("Identity") as Tensor<float>).ReadbackAndClone();

            for (var i = 0; i < PoseSkeleton.KeypointCount; i++)
            {
                var position_ImageSpace = BlazeUtils.mul(M2, new float2(landmarks[5 * i + 0], landmarks[5 * i + 1]));
                var visibility = landmarks[5 * i + 3];

                outPose.Positions[i] = new Vector2(position_ImageSpace.x / textureWidth, position_ImageSpace.y / textureHeight);
                outPose.Visibility[i] = Mathf.Clamp01(visibility);
            }

            outPose.Aspect = textureWidth / (float)textureHeight;
            outPose.IsDetected = true;
            outPose.DetectionScore = outputScore[0];
            return true;
        }

        public void Dispose()
        {
            m_DetectorWorker?.Dispose();
            m_LandmarkerWorker?.Dispose();
            m_DetectorInput?.Dispose();
            m_LandmarkerInput?.Dispose();
        }
    }
}
