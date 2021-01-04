using UnityEngine;
using System.Collections.Generic;

namespace PoseAuthoring.HandAnimation.Models
{
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

        //internal void AddKeys(float keyTime, RVector t)
        //{
        //    xrKeys.Add(new Keyframe(keyTime, t.x));
        //    yrKeys.Add(new Keyframe(keyTime, t.y));
        //    zrKeys.Add(new Keyframe(keyTime, t.z));
        //}

        public List<Keyframe> xrKeys = new List<Keyframe>();
        public List<Keyframe> yrKeys = new List<Keyframe>();
        public List<Keyframe> zrKeys = new List<Keyframe>();
        public List<Keyframe> xlKeys = new List<Keyframe>();
        public List<Keyframe> ylKeys = new List<Keyframe>();
        public List<Keyframe> zlKeys = new List<Keyframe>();

    }
}