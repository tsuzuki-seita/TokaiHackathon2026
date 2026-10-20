using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PoseGame.UI
{
    // Draws a 2D skeleton overlay (dots + bones) inside a RectTransform, from a PersonPose whose
    // positions are normalized [0,1] (bottom-left origin) within that same rect.
    public class SkeletonView
    {
        readonly RectTransform m_Container;
        readonly Dictionary<PoseLandmark, RectTransform> m_Dots = new Dictionary<PoseLandmark, RectTransform>();
        readonly RectTransform[] m_Bones = new RectTransform[PoseSkeleton.Bones.Length];
        readonly CanvasGroup m_Group;

        const float DotSize = 16f;
        const float BoneThickness = 8f;

        public SkeletonView(RectTransform container, Color color)
        {
            m_Container = container;
            m_Group = container.gameObject.AddComponent<CanvasGroup>();
            m_Group.blocksRaycasts = false;
            m_Group.interactable = false;

            var usedLandmarks = new HashSet<PoseLandmark>();
            foreach (var (a, b) in PoseSkeleton.Bones)
            {
                usedLandmarks.Add(a);
                usedLandmarks.Add(b);
            }

            foreach (var landmark in usedLandmarks)
            {
                var dot = UIFactory.CreateDot($"dot_{landmark}", container, color, DotSize);
                dot.gameObject.GetComponent<Image>().raycastTarget = false;
                m_Dots[landmark] = (RectTransform)dot.transform;
            }

            for (var i = 0; i < PoseSkeleton.Bones.Length; i++)
            {
                var bone = UIFactory.CreateDot($"bone_{i}", container, color, 0f);
                bone.gameObject.GetComponent<Image>().raycastTarget = false;
                var rt = (RectTransform)bone.transform;
                rt.pivot = new Vector2(0f, 0.5f);
                m_Bones[i] = rt;
            }
        }

        public bool IsAlive => m_Group != null;

        public void SetVisible(bool visible)
        {
            if (m_Group == null)
                return;
            m_Group.alpha = visible ? 1f : 0f;
        }

        public void UpdateFromPose(PersonPose pose)
        {
            if (!IsAlive)
                return;

            if (!pose.IsDetected)
            {
                SetVisible(false);
                return;
            }
            SetVisible(true);

            var width = m_Container.rect.width;
            var height = m_Container.rect.height;

            foreach (var kv in m_Dots)
            {
                var visible = pose.IsVisible(kv.Key);
                kv.Value.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                var p = pose.Get(kv.Key);
                kv.Value.anchorMin = kv.Value.anchorMax = new Vector2(p.x, p.y);
                kv.Value.anchoredPosition = Vector2.zero;
            }

            for (var i = 0; i < PoseSkeleton.Bones.Length; i++)
            {
                var (a, b) = PoseSkeleton.Bones[i];
                var rt = m_Bones[i];
                var visible = pose.IsVisible(a) && pose.IsVisible(b);
                rt.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                var pa = pose.Get(a);
                var pb = pose.Get(b);
                rt.anchorMin = rt.anchorMax = new Vector2(pa.x, pa.y);
                rt.anchoredPosition = Vector2.zero;

                var delta = new Vector2((pb.x - pa.x) * width, (pb.y - pa.y) * height);
                var length = delta.magnitude;
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                rt.sizeDelta = new Vector2(length, BoneThickness);
                rt.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
