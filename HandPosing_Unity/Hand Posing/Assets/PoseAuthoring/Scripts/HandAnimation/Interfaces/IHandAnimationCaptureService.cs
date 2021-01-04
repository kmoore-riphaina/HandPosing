using Assets.PoseAuthoring.Scripts.HandAnimation.Models;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.HandAnimation.Interfaces
{
    public interface IHandAnimationCaptureService
    {
        Dictionary<double, HandSnapPose> Frames { get; }
        ERecordingState GetRecordingState();
        void ToggleRecordingState();
        void AddFrame(HandSnapPose handSnapPose);
        void ClearFrames();
    }
}