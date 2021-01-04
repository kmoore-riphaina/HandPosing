using PoseAuthoring;
using PoseAuthoring.HandAnimation.Interfaces;
using PoseAuthoring.HandAnimation.Models;
using PoseAuthoring.HandAnimation.VectorGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PoseAuthoring.HandAnimation.AnimationGenerators
{
   public class GenerateFromPoseWithTime : MonoBehaviour, IHandAnimationGenerater
    {
        public Animation GenerateAnimations(IHandAnimationCaptureService _capSvc, HandProvider handProvider, HandGhost AnimationGhost,int clipCount, AnimationVectorAnalyser vectorAnalyser = null)
        {
            var initalGhost = AnimationGhost.gameObject;
            var position = initalGhost.transform.position;
            Animation anim = initalGhost.GetComponent<Animation>();
            if (anim == null)
            {
                anim = initalGhost.AddComponent<Animation>();
            }
            AnimationClip clip = new AnimationClip
            {
                name = clipCount.ToString(),
                legacy = true
            };

            var transformsWithTime = new List<Dictionary<string, KeyValuePair<double, Transform>>>();
            var tempGhosts = new List<HandGhost>();
            foreach (var pose in _capSvc.Frames)
            {
                HandGhost ghost = Instantiate(handProvider.GetHand(pose.Value.handeness), AnimationGhost.transform.position, AnimationGhost.transform.rotation);
                ghost.SetPose(pose.Value, ghost.transform);
                tempGhosts.Add(ghost);
                transformsWithTime.Add(ghost.GetComponentsInChildren<Transform>().ToDictionary(x => x.name, x => new KeyValuePair<double, Transform>(pose.Key, x)));
            }

            BuildKeyframes(clip, transformsWithTime, vectorAnalyser);
            foreach (var g in tempGhosts)
            {
                DestroyImmediate(g.gameObject);
            }
            anim.clip = clip;
            anim.AddClip(clip, clip.name);
            //clipName = clip.name;
            anim.Play(clip.name);
            return anim;
        }

        public void BuildKeyframes(AnimationClip clip, List<Dictionary<string, KeyValuePair<double, Transform>>> transforms, AnimationVectorAnalyser vectorAnalyser)
        {
            Dictionary<string, KeyValuePair<double, AnimationVectors>> name_TimeplusVectors = new Dictionary<string, KeyValuePair<double, AnimationVectors>>();
            for (int i = 0; i < transforms.Count; i++)
            {
                foreach (var change in transforms[i])
                {
                    if (!name_TimeplusVectors.ContainsKey(change.Key))
                    {
                        name_TimeplusVectors.Add(change.Key, new KeyValuePair<double, AnimationVectors>(0, new AnimationVectors()));
                    }
                    var a = change.Value.Value.localRotation;
                    if (a == null || !IsValidVector(a.eulerAngles))
                    {
                        Debug.LogWarning(change.Value.Value.name);
                        continue;
                    }
                    // TODO: add rounding to reduce frame capture rate
                    name_TimeplusVectors[change.Key].Value.AddKeys((float)change.Value.Key, change.Value.Value);
                }
            }


            List<Dictionary<int, VectorAnalysisPoint>> vectorAnalyses = new List<Dictionary<int, VectorAnalysisPoint>>();
            foreach (var v in name_TimeplusVectors)
            {
                if (vectorAnalyser == null)
                {
                    vectorAnalyser = FindObjectOfType<AnimationVectorAnalyser>();
                }
                vectorAnalyses.Add(getVaVectors(v.Key, (float)v.Value.Key, v.Value.Value));
                // curves need to be added after all the keyframes for each transform are made
                AnimationCurve yrCurve = new AnimationCurve(v.Value.Value.yrKeys.ToArray());
                AnimationCurve xrCurve = new AnimationCurve(v.Value.Value.xrKeys.ToArray());
                AnimationCurve zrCurve = new AnimationCurve(v.Value.Value.zrKeys.ToArray());
                AnimationCurve ylCurve = new AnimationCurve(v.Value.Value.ylKeys.ToArray());
                AnimationCurve xlCurve = new AnimationCurve(v.Value.Value.xlKeys.ToArray());
                AnimationCurve zlCurve = new AnimationCurve(v.Value.Value.zlKeys.ToArray());
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].Value.root, transforms[0][v.Key].Value), typeof(Transform), "localEulerAnglesRaw.y", yrCurve);
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].Value.root, transforms[0][v.Key].Value), typeof(Transform), "localEulerAnglesRaw.x", xrCurve);
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].Value.root, transforms[0][v.Key].Value), typeof(Transform), "localEulerAnglesRaw.z", zrCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.y", ylCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.x", xlCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.z", zlCurve);
            }
            if (vectorAnalyser != null)
            {
                vectorAnalyser.GraphVectorsNew(vectorAnalyses);
            }
        }
        private Dictionary<int, VectorAnalysisPoint> getVaVectors(string vectorName, float timeRef, AnimationVectors value)
        {
            var results = new Dictionary<int, VectorAnalysisPoint>();
            for (int i = 0; i + 1 < value.xrKeys.Count; i++)
            {
                results.Add(i, new VectorAnalysisPoint { XvectorName = vectorName, ZTimeReference = timeRef, YTotalDelta = ExtractDelta(value, i) / 100 }); ;
            }
            return results;
        }

        private static bool IsValidVector(Vector3 a)
        {
            if (float.IsNaN(a.x) || float.IsInfinity(a.x) || float.IsNaN(a.y) || float.IsInfinity(a.y) || float.IsNaN(a.z) || float.IsInfinity(a.z))
            {
                return false;
            }
            return true;
        }
        private static float ExtractDelta(AnimationVectors value, int i)
        {
            return Math.Abs(value.xrKeys[i].value - value.xrKeys[i + 1].value) + Math.Abs(value.yrKeys[i].value - value.yrKeys[i + 1].value) + Math.Abs(value.zrKeys[i].value - value.zrKeys[i + 1].value);
        }
        private string GetRelativePath(Transform root, Transform tr)
        {
            List<string> pths = new List<string>();
            Transform t = tr;
            while (t != root && t != t.root)
            {
                pths.Add(t.name);
                t = t.parent;
            }

            pths.Reverse();
            return string.Join("/", pths);
        }
    }
}
