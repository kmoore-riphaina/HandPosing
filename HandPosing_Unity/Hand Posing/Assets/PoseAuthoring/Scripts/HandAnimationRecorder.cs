using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System;
using Oculus.Platform;

namespace PoseAuthoring
{
    public class HandAnimationRecorder : MonoBehaviour
    {
        // Hand Animator
        [SerializeField]
        public HandProvider handProvider;

        public float InitialFrameRate;
        public float targetFrameRate = 60f;
        public bool LeftHand = true;

        private Animation lastAnimation;
        private string clipName;

        [SerializeField]
        private HandPuppet puppetHand;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;
        private KeyCode replayKey = KeyCode.RightShift;
        private KeyCode deleteKey = KeyCode.Delete;

        private HandGhost AnimationGhost;
        private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private bool Recording;

        private List<HandSnapPose> ghosts = new List<HandSnapPose>();

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
                    Debug.Log(string.Format("{0} frames in {2} seconds =  captured at {1} fps", ghosts.Count, ghosts.Count / timer.Elapsed.TotalSeconds, timer.Elapsed.TotalSeconds));
                    if (ghosts.Count > 0)
                    {
                        GenerateAnimations((float)(ghosts.Count / timer.Elapsed.TotalSeconds));
                        ghosts = new List<HandSnapPose>();
                    }
                }
                else
                {
                    timer.Start();
                    UnityEngine.Application.targetFrameRate = (int)targetFrameRate;
                    RecordPose();
                }
            }
            if (Input.GetKeyDown(deleteKey) && AnimationGhost != null)
            {

                lastAnimation = null;
                DestroyImmediate(AnimationGhost);
                ghosts = new List<HandSnapPose>();
            }

            if (lastAnimation != null && Input.GetKeyDown(replayKey))
            {
                lastAnimation.Play(clipName);
            }
        }

        private void AddAnimationPose(HandPuppet puppet)
        {
            HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
            if (AnimationGhost == null)
            {
                HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), this.transform.position, this.transform.rotation);
                ghost.SetPose(pose, ghost.transform);
                ghosts.Add(puppet.CurrentPoseVisual(this.transform));
                AnimationGhost = ghost;
            }
            else
            {
                AnimationGhost.SetPose(pose, AnimationGhost.transform);
                ghosts.Add(pose);
            }
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


        private void GenerateAnimations(float framerate)
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
                name = "clip0",
                legacy = true
            };

            var transforms = new List<Dictionary<string, Transform>>();
            var tempGhosts = new List<HandGhost>();
            foreach (var pose in ghosts)
            {
                HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), AnimationGhost.transform.position, AnimationGhost.transform.rotation);
                ghost.SetPose(pose, ghost.transform);
                tempGhosts.Add(ghost);
                transforms.Add(ghost.GetComponentsInChildren<Transform>().ToDictionary(x => x.name, x => x));
            }

            BuildKeyframes(clip, transforms, framerate);
            foreach(var g in tempGhosts)
            {
                DestroyImmediate(g.gameObject);
            }
            anim.clip = clip;
            anim.AddClip(clip, clip.name);
            anim.Play(clip.name);
            clipName = clip.name;
            lastAnimation = anim;
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

        public void RecordPose()
        {
            AddAnimationPose(puppetHand);
            Time.captureFramerate = UnityEngine.Application.targetFrameRate;
            Recording = true;
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