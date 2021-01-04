using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PoseAuthoring.HandAnimation
{
    public class AnimationPreviewBox : MonoBehaviour
    {
        [SerializeField]
        public Slider slider;
        public HandAnimationRecorder a;

        void Update()
        {
            if (a != null && a.lastAnimation != null)
            {
                // update the slide based on the animation position
                slider.normalizedValue = a.lastAnimation[a.clipName].normalizedTime;
            }
        }

        //public void SetAnimation(HandAnimationRecorder rec)
        //{
        //    _clipName = clipName;
        //    anim = anim1 ?? throw new ArgumentNullException(nameof(anim));
        //    //anim[_clipName].speed = 0;
        //}
    }
}