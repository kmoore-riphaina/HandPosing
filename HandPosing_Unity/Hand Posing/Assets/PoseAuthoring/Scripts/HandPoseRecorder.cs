﻿using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System;
using Oculus.Platform;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        // Hand Animator
        [SerializeField]
        public HandProvider handProvider;

        private List<Dictionary<string, RVector>> handVectorsCollection;
        private List<HandGhost> frames = new List<HandGhost>();
        public float InitialFrameRate;
        public float targetFrameRate = 60f;
        public bool LeftHand = true;

        private Animation lastAnimation;
        private string clipName;

        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private Grabber grabber;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;
        private KeyCode replayKey = KeyCode.RightShift;
        private KeyCode deleteKey = KeyCode.Delete;

        private HandGhost AnimationGhost;
        private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private bool Recording;
        private HandGhost previousGhost;

        private List<HandSnapPose> handSnapPoses = new List<HandSnapPose>();

        //private Enum TrackMechanism
        //{
        //    ghost, pose
        //}
        private void Start()
        {
            InitialFrameRate = UnityEngine.Application.targetFrameRate;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                AddAnimationPose(puppetHand);
            }
            if (Input.GetKeyDown(recordKey) || Recording)
            {
                if (Input.GetKeyDown(recordKey) && Recording)
                {
                    Recording = false;
                    timer.Stop();
                    Debug.Log(string.Format("{0} frames in {2} seconds =  captured at {1} fps", frames.Count, frames.Count / timer.Elapsed.TotalSeconds, timer.Elapsed.TotalSeconds));
                    if (frames.Count > 0)
                    {
                        GenerateAnimations(frames, (float)(frames.Count / timer.Elapsed.TotalSeconds));
                        frames = new List<HandGhost>();
                    }
                }
                else
                {
                    timer.Start();
                    UnityEngine.Application.targetFrameRate = (int)targetFrameRate;
                    RecordPose();
                }
            }
            if (Input.GetKeyDown(deleteKey) && previousGhost != null)
            {
                foreach (var g in frames)
                {
                    DestroyImmediate(g.gameObject);
                }
                lastAnimation = null;
                DestroyImmediate(previousGhost);
                frames = new List<HandGhost>();
            }

            if (lastAnimation != null && Input.GetKeyDown(replayKey))
            {
                lastAnimation.Play(clipName);
            }
            HighlightNearestPose();
        }

        public struct RVector
        {
            public float x;
            public float y;
            public float z;

            public RVector(Transform t)
            {
                this.x = t.localRotation.eulerAngles.x;
                this.y = t.localRotation.eulerAngles.y;
                this.z = t.localRotation.eulerAngles.z;
            }
        }


        private void GenerateAnimations(List<HandGhost> ghosts, float framerate)
        {
            var initalGhost = ghosts[0].gameObject;
            var position = initalGhost.transform.position;
            Animation anim = initalGhost.GetComponent<Animation>();
            if (anim == null)
            {
                anim = initalGhost.AddComponent<Animation>();
            }
            AnimationClip clip = new AnimationClip
            {
                legacy = true
            };

            var transforms = new List<Dictionary<string, Transform>>();
            foreach (var g in ghosts)
            {
                transforms.Add(g.GetComponentsInChildren<Transform>().ToDictionary(x => x.name, x => x));
            }
            BuildKeyframes(clip, transforms, framerate);

            foreach (var g in ghosts)
            {
                if (ghosts.IndexOf(g) != 0)
                {
                    UnityEngine.Object.DestroyImmediate(g.gameObject);
                }
            }
            if (anim.clip == null)
            {
                anim.clip = clip;
                anim.AddClip(clip, clip.name);
                anim.Play(clip.name);
                lastAnimation = anim;
                clipName = clip.name;
            }
        }



        private void BuildKeyframes(AnimationClip clip, List<Dictionary<string, Transform>> transforms, float framerate)
        {
            Dictionary<string, AnimationVectors> valuePairs = new Dictionary<string, AnimationVectors>();
            float frameRateMultiplier = transforms.Count / framerate / 100;
            Debug.Log(string.Format("building animations at {0} between frames", frameRateMultiplier));
            float keyTime = 0.0f;
            for (int i = 0; i < transforms.Count; i++)
            {
                foreach (var change in transforms[i])
                {
                    if (!valuePairs.ContainsKey(change.Key))
                    {
                        valuePairs.Add(change.Key, new AnimationVectors());
                    }
                    var a = change.Value.localRotation;
                    if (a == null || !IsValidVector(a.eulerAngles))
                    {
                        Debug.LogWarning(change.Value.name);
                        continue;
                    }
                    valuePairs[change.Key].AddKeys(keyTime, change.Value);
                }
                keyTime += frameRateMultiplier;
            }

            foreach (var v in valuePairs)
            {
                // curves need to be added after all the keyframes for each transform are made
                AnimationCurve yrCurve = new AnimationCurve(v.Value.yrKeys.ToArray());
                AnimationCurve xrCurve = new AnimationCurve(v.Value.xrKeys.ToArray());
                AnimationCurve zrCurve = new AnimationCurve(v.Value.zrKeys.ToArray());
                AnimationCurve ylCurve = new AnimationCurve(v.Value.ylKeys.ToArray());
                AnimationCurve xlCurve = new AnimationCurve(v.Value.xlKeys.ToArray());
                AnimationCurve zlCurve = new AnimationCurve(v.Value.zlKeys.ToArray());
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].root, transforms[0][v.Key]), typeof(Transform), "localEulerAnglesRaw.y", yrCurve);
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].root, transforms[0][v.Key]), typeof(Transform), "localEulerAnglesRaw.x", xrCurve);
                clip.SetCurve(GetRelativePath(transforms[0][v.Key].root, transforms[0][v.Key]), typeof(Transform), "localEulerAnglesRaw.z", zrCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.y", ylCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.x", xlCurve);
                //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.z", zlCurve);
            }
        }

        private static bool IsValidVector(Vector3 a)
        {
            if (float.IsNaN(a.x) || float.IsInfinity(a.x) || float.IsNaN(a.y) || float.IsInfinity(a.y) || float.IsNaN(a.z) || float.IsInfinity(a.z))
            {
                return false;
            }
            return true;
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

        private void HighlightNearestPose()
        {
            var grabbable = grabber.FindClosestGrabbable().Item1;

            if (grabbable != null && grabbable.Snappable != null)
            {
                HandSnapPose userPose = this.puppetHand.CurrentPoseTracked(grabbable.Snappable.transform);
                HandGhost ghost = grabbable.Snappable.FindNearsetGhost(userPose, out float score, out var bestPose);
                if (ghost != previousGhost)
                {
                    previousGhost?.Highlight(false);
                    previousGhost = ghost;
                }
                ghost?.Highlight(score);
            }
            else if (previousGhost != null)
            {
                previousGhost.Highlight(false);
                previousGhost = null;
            }
        }

        public void RecordPose()
        {
            Grabbable grabbable = grabber.FindClosestGrabbable().Item1;
            if (grabbable == null)
            {
                AddAnimationPose(puppetHand);
                Time.captureFramerate = UnityEngine.Application.targetFrameRate;
                Recording = true;
            }
            else
            {
                grabbable?.Snappable?.AddPose(puppetHand);
            }
        }

        private void AddAnimationPose(HandPuppet puppet)
        {
            if (AnimationGhost == null)
            {
                HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
                HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), this.transform.position, this.transform.rotation);
                ghost.SetPose(pose, ghost.transform);
                handSnapPoses.Add(puppet.CurrentPoseTracked(this.transform));
                AnimationGhost = ghost;
            }
            else
            {
                HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
                HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), this.transform.position, this.transform.rotation);
                ghost.SetPose(pose, ghost.transform);
                frames.Add(ghost);
            }
        }


        public class AnimationVectors
        {
            public void AddKeys(float keyTime, Transform t)
            {
                xrKeys.Add(new Keyframe(keyTime, t.localRotation.eulerAngles.x));
                yrKeys.Add(new Keyframe(keyTime, t.localRotation.eulerAngles.y));
                zrKeys.Add(new Keyframe(keyTime, t.localRotation.eulerAngles.z));
                xlKeys.Add(new Keyframe(keyTime, t.localPosition.x));
                ylKeys.Add(new Keyframe(keyTime, t.localPosition.y));
                zlKeys.Add(new Keyframe(keyTime, t.localPosition.z));
            }

            internal void AddKeys(float keyTime, RVector t)
            {
                xrKeys.Add(new Keyframe(keyTime, t.x));
                yrKeys.Add(new Keyframe(keyTime, t.y));
                zrKeys.Add(new Keyframe(keyTime, t.z));
            }

            public List<Keyframe> xrKeys = new List<Keyframe>();
            public List<Keyframe> yrKeys = new List<Keyframe>();
            public List<Keyframe> zrKeys = new List<Keyframe>();
            public List<Keyframe> xlKeys = new List<Keyframe>();
            public List<Keyframe> ylKeys = new List<Keyframe>();
            public List<Keyframe> zlKeys = new List<Keyframe>();

        }


        //private void GenerateAnimations(GameObject sourceGhostHand, List<Dictionary<string, RVector>> handVectors, float framerate)
        //{

        //    Animation anim = sourceGhostHand.GetComponent<Animation>();
        //    if (anim == null)
        //    {
        //        anim = sourceGhostHand.AddComponent<Animation>();
        //    }
        //    AnimationClip clip = new AnimationClip
        //    {
        //        legacy = true
        //    };

        //    // specify parent
        //    BuildKeyframes(clip, handVectors, framerate);

        //    // always set next clip
        //    anim.clip = clip;
        //    anim.AddClip(clip, clip.name);
        ////    anim.Play(clip.name);
        ////    lastAnimation = anim;
        ////    clipName = clip.name;
        ////}

        //private void BuildKeyframes(AnimationClip clip, List<Dictionary<string, RVector>> handVectors, float framerate)
        //{
        //    Dictionary<string, Tuple<string, AnimationVectors>> valuePairs = new Dictionary<string, Tuple<string, AnimationVectors>>(); 

        //    float frameRateMultiplier = (((float)handVectors.Count / framerate) / 100);
        //    float keyTime = 0.0f;
        //    for (int i = 0; i < handVectors.Count; i++)
        //    {
        //        foreach (var change in handVectors[i])
        //        {
        //            if (!valuePairs.ContainsKey(change.Key))
        //            {
        //                valuePairs.Add(change.Key, new AnimationVectors());
        //            }
        //            valuePairs[change.Key].AddKeys(keyTime, change.Value);
        //        }
        //        keyTime += frameRateMultiplier;
        //    }
        //    foreach (var v in valuePairs)
        //    {
        //        if (v.Value.yrKeys.Count > 0)
        //        {
        //            // curves need to be added after all the keyframes for each transform are made
        //            AnimationCurve yrCurve = new AnimationCurve(v.Value.yrKeys.ToArray());
        //            AnimationCurve xrCurve = new AnimationCurve(v.Value.xrKeys.ToArray());
        //            AnimationCurve zrCurve = new AnimationCurve(v.Value.zrKeys.ToArray());
        //            AnimationCurve ylCurve = new AnimationCurve(v.Value.ylKeys.ToArray());
        //            AnimationCurve xlCurve = new AnimationCurve(v.Value.xlKeys.ToArray());
        //            AnimationCurve zlCurve = new AnimationCurve(v.Value.zlKeys.ToArray());
        //            clip.SetCurve(v.Key.Item2, typeof(Transform), "localEulerAnglesRaw.y", yrCurve);
        //            clip.SetCurve(v.Key.Item2, typeof(Transform), "localEulerAnglesRaw.x", xrCurve);
        //            clip.SetCurve(v.Key.Item2, typeof(Transform), "localEulerAnglesRaw.z", zrCurve);
        //            //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.y", ylCurve);
        //            //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.x", xlCurve);
        //            //clip.SetCurve(GetRelativePath(orig[v.Key].root, orig[v.Key]), typeof(Transform), "localPosition.z", zlCurve);
        //        }
        //    }
        //}
    }
}