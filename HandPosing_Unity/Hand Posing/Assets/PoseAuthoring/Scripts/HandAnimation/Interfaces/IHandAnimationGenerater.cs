using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.HandAnimation.Interfaces
{
    public interface IHandAnimationGenerater
    {
        void BuildKeyframes(AnimationClip clip, List<Dictionary<string, KeyValuePair<double, Transform>>> transforms, AnimationVectorAnalyser vectorAnalyser);
        Animation GenerateAnimations(IHandAnimationCaptureService _capSvc, HandProvider handProvider, HandGhost AnimationGhost, int clipcount, AnimationVectorAnalyser vectorAnalyser = null);
    }
}